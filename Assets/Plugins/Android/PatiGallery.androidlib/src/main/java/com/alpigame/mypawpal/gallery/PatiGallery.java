package com.alpigame.mypawpal.gallery;

import android.app.Activity;
import android.content.ContentResolver;
import android.content.ContentValues;
import android.net.Uri;
import android.os.Build;
import android.os.Environment;
import android.provider.MediaStore;

import java.io.OutputStream;

/** Foto stüdyosu: PNG'yi galeriye (Resimler/Alpis Paw Pal) kaydeder. Sonuç: "ok", "old" (Android 9 ve öncesi) ya da "error". */
public final class PatiGallery {

    private PatiGallery() {
    }

    public static String save(Activity activity, byte[] png, String name) {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.Q) return "old";
        try {
            ContentValues values = new ContentValues();
            values.put(MediaStore.Images.Media.DISPLAY_NAME, name + ".png");
            values.put(MediaStore.Images.Media.MIME_TYPE, "image/png");
            values.put(MediaStore.Images.Media.RELATIVE_PATH, Environment.DIRECTORY_PICTURES + "/Alpis Paw Pal");
            ContentResolver resolver = activity.getContentResolver();
            Uri uri = resolver.insert(MediaStore.Images.Media.EXTERNAL_CONTENT_URI, values);
            if (uri == null) return "error";
            try (OutputStream out = resolver.openOutputStream(uri)) {
                if (out == null) return "error";
                out.write(png);
            }
            return "ok";
        } catch (Exception e) {
            return "error";
        }
    }
}
