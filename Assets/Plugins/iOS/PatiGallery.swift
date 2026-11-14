// Fotoğraflar'a PNG kaydeder (yalnızca ekleme izni). C# tarafı: Assets/Scripts/Core/Gallery.cs
//
// PatiGallery_Save dönüşü: 0 kaydediliyor (izin zaten var), 1 izin soruluyor (sonuç geri çağrıyla gelir), 2 izin reddedilmiş.
// Geri çağrı sonuçları: "ok", "denied", "error". Ana kuyruktan çağrılır.
import Foundation
import Photos

public typealias PatiGalleryCallback = @convention(c) (UnsafePointer<CChar>?) -> Void

private var patiGalleryCallback: PatiGalleryCallback?

private func patiGalleryEmit(_ result: String) {
    DispatchQueue.main.async {
        guard let cb = patiGalleryCallback else { return }
        result.withCString { cb($0) }
    }
}

private func patiGallerySave(_ data: Data) {
    PHPhotoLibrary.shared().performChanges({
        let request = PHAssetCreationRequest.forAsset()
        request.addResource(with: .photo, data: data, options: nil)
    }, completionHandler: { success, _ in
        patiGalleryEmit(success ? "ok" : "error")
    })
}

@_cdecl("PatiGallery_SetCallback")
public func patiGallerySetCallback(_ callback: PatiGalleryCallback?) {
    patiGalleryCallback = callback
}

@_cdecl("PatiGallery_Save")
public func patiGallerySaveEntry(_ bytes: UnsafePointer<UInt8>?, _ length: Int32) -> Int32 {
    guard let bytes = bytes, length > 0 else { return 2 }
    // Veri hemen kopyalanır: Unity dizisi bu çağrıdan sonra serbest kalabilir
    let data = Data(bytes: bytes, count: Int(length))
    switch PHPhotoLibrary.authorizationStatus(for: .addOnly) {
    case .authorized, .limited:
        patiGallerySave(data)
        return 0
    case .notDetermined:
        PHPhotoLibrary.requestAuthorization(for: .addOnly) { status in
            if status == .authorized || status == .limited {
                patiGallerySave(data)
            } else {
                patiGalleryEmit("denied")
            }
        }
        return 1
    default:
        return 2
    }
}
