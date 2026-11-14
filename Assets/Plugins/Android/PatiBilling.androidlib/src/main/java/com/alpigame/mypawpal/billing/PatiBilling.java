package com.alpigame.mypawpal.billing;

import android.app.Activity;

import com.android.billingclient.api.AcknowledgePurchaseParams;
import com.android.billingclient.api.BillingClient;
import com.android.billingclient.api.BillingClientStateListener;
import com.android.billingclient.api.BillingFlowParams;
import com.android.billingclient.api.BillingResult;
import com.android.billingclient.api.ConsumeParams;
import com.android.billingclient.api.PendingPurchasesParams;
import com.android.billingclient.api.ProductDetails;
import com.android.billingclient.api.Purchase;
import com.android.billingclient.api.PurchasesUpdatedListener;
import com.android.billingclient.api.QueryProductDetailsParams;
import com.android.billingclient.api.QueryPurchasesParams;

import org.json.JSONArray;
import org.json.JSONObject;

import java.util.ArrayList;
import java.util.Collections;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

/**
 * Google Play Billing köprüsü. Unity (C#) bir Listener verir; her olay (tür, JSON) olarak geri gider:
 *  ready {code}, products {code, products:[{id, price}]}, purchase/pending {ids, token, acknowledged, restore},
 *  failed {id, code}, restored {code}, consumed {code}, acknowledged {code}, disconnected {}.
 * Hakkı vermek, sonra consume/acknowledge çağırmak Unity tarafının işidir (StoreService).
 */
public class PatiBilling implements PurchasesUpdatedListener {

    public interface Listener {
        void onEvent(String type, String json);
    }

    private final Activity activity;
    private final Listener listener;
    private final BillingClient client;
    private final Map<String, ProductDetails> details = new HashMap<>();

    public PatiBilling(Activity activity, Listener listener) {
        this.activity = activity;
        this.listener = listener;
        client = BillingClient.newBuilder(activity)
                .setListener(this)
                .enablePendingPurchases(PendingPurchasesParams.newBuilder().enableOneTimeProducts().build())
                .enableAutoServiceReconnection()
                .build();
    }

    public void connect() {
        if (client.isReady()) {
            emit("ready", "{\"code\":0}");
            return;
        }
        client.startConnection(new BillingClientStateListener() {
            @Override
            public void onBillingSetupFinished(BillingResult result) {
                emit("ready", code(result));
            }

            @Override
            public void onBillingServiceDisconnected() {
                emit("disconnected", "{}");
            }
        });
    }

    /** Virgülle ayrılmış tek seferlik ürünlerin ve aboneliklerin yerel fiyatlarını sorar (iki "products" olayı). */
    public void queryProducts(String inappCsv, String subsCsv) {
        query(inappCsv, BillingClient.ProductType.INAPP);
        if (subsCsv != null && !subsCsv.isEmpty()) query(subsCsv, BillingClient.ProductType.SUBS);
    }

    private void query(String csv, String type) {
        List<QueryProductDetailsParams.Product> products = new ArrayList<>();
        for (String id : csv.split(",")) {
            products.add(QueryProductDetailsParams.Product.newBuilder()
                    .setProductId(id)
                    .setProductType(type)
                    .build());
        }
        QueryProductDetailsParams params = QueryProductDetailsParams.newBuilder().setProductList(products).build();
        client.queryProductDetailsAsync(params, (billingResult, queryResult) -> {
            JSONObject out = new JSONObject();
            try {
                JSONArray array = new JSONArray();
                for (ProductDetails d : queryResult.getProductDetailsList()) {
                    details.put(d.getProductId(), d);
                    JSONObject item = new JSONObject();
                    item.put("id", d.getProductId());
                    item.put("price", priceOf(d));
                    array.put(item);
                }
                out.put("code", billingResult.getResponseCode());
                out.put("products", array);
            } catch (Exception ignored) {
            }
            emit("products", out.toString());
        });
    }

    /** Tek seferlikte teklif fiyatı; abonelikte ilk teklifin son (deneme sonrası) fiyatı. */
    private static String priceOf(ProductDetails d) {
        ProductDetails.OneTimePurchaseOfferDetails offer = d.getOneTimePurchaseOfferDetails();
        if (offer != null) return offer.getFormattedPrice();
        ProductDetails.SubscriptionOfferDetails sub = bestOffer(d);
        if (sub == null) return "";
        List<ProductDetails.PricingPhase> phases = sub.getPricingPhases().getPricingPhaseList();
        return phases.isEmpty() ? "" : phases.get(phases.size() - 1).getFormattedPrice();
    }

    /**
     * Abonelik teklifi: en çok aşamalı olan (ücretsiz deneme + temel plan). Play yalnızca kullanıcının hak
     * kazandığı teklifleri listeler ve sırasını garanti etmez; deneme hakkı yoksa temel plan kalır.
     */
    private static ProductDetails.SubscriptionOfferDetails bestOffer(ProductDetails d) {
        List<ProductDetails.SubscriptionOfferDetails> offers = d.getSubscriptionOfferDetails();
        if (offers == null || offers.isEmpty()) return null;
        ProductDetails.SubscriptionOfferDetails best = offers.get(0);
        for (ProductDetails.SubscriptionOfferDetails offer : offers) {
            if (offer.getPricingPhases().getPricingPhaseList().size() > best.getPricingPhases().getPricingPhaseList().size()) best = offer;
        }
        return best;
    }

    public void buy(final String productId) {
        ProductDetails d = details.get(productId);
        if (d == null) {
            emit("failed", "{\"id\":\"" + productId + "\",\"code\":-100}");
            return;
        }
        BillingFlowParams.ProductDetailsParams.Builder item = BillingFlowParams.ProductDetailsParams.newBuilder().setProductDetails(d);
        // Abonelikte teklif (deneme süresi dahil) seçilmeli
        ProductDetails.SubscriptionOfferDetails offer = bestOffer(d);
        if (offer != null) item.setOfferToken(offer.getOfferToken());
        final BillingFlowParams params = BillingFlowParams.newBuilder()
                .setProductDetailsParamsList(Collections.singletonList(item.build()))
                .build();
        activity.runOnUiThread(() -> {
            BillingResult result = client.launchBillingFlow(activity, params);
            if (result.getResponseCode() != BillingClient.BillingResponseCode.OK) {
                emit("failed", "{\"id\":\"" + productId + "\",\"code\":" + result.getResponseCode() + "}");
            }
        });
    }

    @Override
    public void onPurchasesUpdated(BillingResult result, List<Purchase> purchases) {
        if (result.getResponseCode() == BillingClient.BillingResponseCode.OK && purchases != null) {
            for (Purchase p : purchases) report(p, false);
        } else {
            emit("failed", "{\"id\":\"\",\"code\":" + result.getResponseCode() + "}");
        }
    }

    /**
     * Sahip olunan (ve yarım kalmış) alımları yeniden bildirir: kalıcılar geri yüklenir, bekleyenler tamamlanır,
     * etkin abonelik yeniden doğrulanır. İki sorgu da bitince tek "restored" gider (abonelik yoksa kulüp kapanır).
     */
    public void restore() {
        final int[] pending = {2};
        final int[] worst = {BillingClient.BillingResponseCode.OK};
        for (String type : new String[] {BillingClient.ProductType.INAPP, BillingClient.ProductType.SUBS}) {
            QueryPurchasesParams params = QueryPurchasesParams.newBuilder().setProductType(type).build();
            client.queryPurchasesAsync(params, (result, purchases) -> {
                if (purchases != null) {
                    for (Purchase p : purchases) report(p, true);
                }
                synchronized (pending) {
                    if (result.getResponseCode() != BillingClient.BillingResponseCode.OK) worst[0] = result.getResponseCode();
                    if (--pending[0] == 0) emit("restored", "{\"code\":" + worst[0] + "}");
                }
            });
        }
    }

    public void consume(String token) {
        client.consumeAsync(ConsumeParams.newBuilder().setPurchaseToken(token).build(),
                (result, t) -> emit("consumed", code(result)));
    }

    public void acknowledge(String token) {
        client.acknowledgePurchase(AcknowledgePurchaseParams.newBuilder().setPurchaseToken(token).build(),
                result -> emit("acknowledged", code(result)));
    }

    private void report(Purchase p, boolean restore) {
        JSONObject o = new JSONObject();
        try {
            o.put("ids", new JSONArray(p.getProducts()));
            o.put("token", p.getPurchaseToken());
            o.put("acknowledged", p.isAcknowledged());
            o.put("restore", restore);
        } catch (Exception ignored) {
        }
        boolean purchased = p.getPurchaseState() == Purchase.PurchaseState.PURCHASED;
        emit(purchased ? "purchase" : "pending", o.toString());
    }

    private static String code(BillingResult result) {
        return "{\"code\":" + result.getResponseCode() + "}";
    }

    private void emit(String type, String json) {
        try {
            listener.onEvent(type, json);
        } catch (Exception ignored) {
        }
    }
}
