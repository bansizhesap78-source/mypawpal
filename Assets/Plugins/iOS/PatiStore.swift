// StoreKit 2 köprüsü (iOS 15+). C# tarafı: Assets/Scripts/Store/AppleAppStore.cs
//
// Olaylar (type, json) çiftiyle Unity'ye gider; her zaman ana kuyruktan çağrılır. Gövdeler Google Play köprüsüyle aynıdır:
//   ready     {"code":0}                                  0: tamam, 1: ürünler alınamadı (ağ yok)
//   products  {"code":0,"products":[{"id","price"}]}      price: displayPrice (yerel para birimi)
//   purchase  {"ids":[id],"token":"..","acknowledged":b,"restore":b}
//   pending   {"ids":[id]}                                Ask to Buy / onay bekliyor
//   failed    {"code":1|2,"id":".."}                      1: kullanıcı vazgeçti, 2: hata
//   restored  {"code":0|1}                                sahip olunan alımların listesi bitti
import Foundation
import StoreKit

public typealias PatiStoreCallback = @convention(c) (UnsafePointer<CChar>?, UnsafePointer<CChar>?) -> Void

final class PatiStore {
    static let shared = PatiStore()

    private let lock = NSLock()
    private var callback: PatiStoreCallback?
    private var products: [String: Product] = [:]
    private var unfinished: [String: Transaction] = [:]
    private var listening = false

    // Kilit yalnızca eşzamanlı yardımcılarda tutulur (async bağlamda NSLock kullanılmaz)
    private func lookup(_ id: String) -> Product? {
        lock.lock(); defer { lock.unlock() }
        return products[id]
    }

    private func remember(_ list: [Product]) {
        lock.lock(); defer { lock.unlock() }
        for p in list { products[p.id] = p }
    }

    private func stash(_ token: String, _ transaction: Transaction) {
        lock.lock(); defer { lock.unlock() }
        unfinished[token] = transaction
    }

    private func take(_ token: String) -> Transaction? {
        lock.lock(); defer { lock.unlock() }
        return unfinished.removeValue(forKey: token)
    }

    /// Dinleyici ilk kez başlatılıyorsa true döner.
    private func claimListening() -> Bool {
        lock.lock(); defer { lock.unlock() }
        if listening { return false }
        listening = true
        return true
    }

    func setCallback(_ cb: PatiStoreCallback?) {
        lock.lock(); callback = cb; lock.unlock()
    }

    // MARK: Unity'ye olay

    private func emit(_ type: String, _ body: [String: Any]) {
        guard let data = try? JSONSerialization.data(withJSONObject: body),
              let json = String(data: data, encoding: .utf8) else { return }
        DispatchQueue.main.async { [self] in
            lock.lock(); let cb = callback; lock.unlock()
            guard let cb = cb else { return }
            type.withCString { t in json.withCString { j in cb(t, j) } }
        }
    }

    // MARK: Bağlantı

    func connect(_ ids: [String]) {
        startListening()
        Task { [self] in
            do {
                let list = try await Product.products(for: ids)
                remember(list)
                emit("ready", ["code": 0])
                emit("products", [
                    "code": 0,
                    "products": list.map { ["id": $0.id, "price": $0.displayPrice] }
                ])
            } catch {
                emit("ready", ["code": 1])
                return
            }
            // Yarım kalan alımlar (uygulama kapalıyken tamamlananlar, Ask to Buy onayı)
            Task { [self] in
                for await result in Transaction.unfinished { await handle(result, restore: false) }
            }
            await sweepEntitlements()
            emit("restored", ["code": 0])
        }
    }

    /// Transaction.updates uygulama açıkken gelen her işlemi (onaylanan Ask to Buy, yenilenen abonelik) yakalar.
    private func startListening() {
        if !claimListening() { return }
        Task { [self] in
            for await result in Transaction.updates { await handle(result, restore: false) }
        }
    }

    // MARK: Satın alma

    func buy(_ id: String) {
        Task { [self] in
            var product = lookup(id)
            if product == nil {
                product = try? await Product.products(for: [id]).first
                if let p = product { remember([p]) }
            }
            guard let product = product else {
                emit("failed", ["code": 2, "id": id])
                return
            }
            do {
                switch try await product.purchase() {
                case .success(let verification):
                    switch verification {
                    case .verified(let transaction):
                        report(transaction, restore: false, finished: false)
                    case .unverified:
                        emit("failed", ["code": 2, "id": id])
                    }
                case .pending:
                    emit("pending", ["ids": [id]])
                case .userCancelled:
                    emit("failed", ["code": 1, "id": id])
                @unknown default:
                    emit("failed", ["code": 2, "id": id])
                }
            } catch {
                emit("failed", ["code": 2, "id": id])
            }
        }
    }

    // MARK: Geri yükleme

    func restore() {
        Task { [self] in
            var code = 0
            do {
                try await AppStore.sync()
            } catch StoreKitError.userCancelled {
                code = 0
            } catch {
                code = 1
            }
            await sweepEntitlements()
            emit("restored", ["code": code])
        }
    }

    /// Kalıcı ürünler ve etkin abonelik. Tüketilir ürünler burada çıkmaz; süresi bitmiş abonelik de çıkmaz,
    /// C# tarafı "abonelik görülmedi" diyerek kulübü kapatır.
    private func sweepEntitlements() async {
        for await result in Transaction.currentEntitlements {
            await handle(result, restore: true)
        }
    }

    // MARK: İşlem

    private func handle(_ result: VerificationResult<Transaction>, restore: Bool) async {
        guard case .verified(let transaction) = result else { return }
        report(transaction, restore: restore, finished: restore)
    }

    private func report(_ transaction: Transaction, restore: Bool, finished: Bool) {
        // İade edilmiş ya da geri alınmış işlem hak vermez
        if transaction.revocationDate != nil { return }
        let token = String(transaction.id)
        if !finished {
            stash(token, transaction)
        }
        emit("purchase", [
            "ids": [transaction.productID],
            "token": token,
            "acknowledged": finished,
            "restore": restore
        ])
    }

    /// Hak verildikten sonra C# çağırır: işlem Apple'a "teslim edildi" diye bildirilir.
    func finish(_ token: String) {
        guard let transaction = take(token) else { return }
        Task { await transaction.finish() }
    }
}

// MARK: C girişleri (DllImport("__Internal"))

@_cdecl("PatiStore_SetCallback")
public func patiStoreSetCallback(_ callback: PatiStoreCallback?) {
    PatiStore.shared.setCallback(callback)
}

@_cdecl("PatiStore_Connect")
public func patiStoreConnect(_ ids: UnsafePointer<CChar>?) {
    let list = ids.map { String(cString: $0) }?
        .split(separator: ",").map(String.init).filter { !$0.isEmpty } ?? []
    PatiStore.shared.connect(list)
}

@_cdecl("PatiStore_Buy")
public func patiStoreBuy(_ id: UnsafePointer<CChar>?) {
    guard let id = id else { return }
    PatiStore.shared.buy(String(cString: id))
}

@_cdecl("PatiStore_Restore")
public func patiStoreRestore() {
    PatiStore.shared.restore()
}

@_cdecl("PatiStore_Finish")
public func patiStoreFinish(_ token: UnsafePointer<CChar>?) {
    guard let token = token else { return }
    PatiStore.shared.finish(String(cString: token))
}
