# LightenUp — Panduan Setup & Instalasi

## Prasyarat

| Kebutuhan | Versi minimal |
|---|---|
| .NET SDK | 10.0 |
| SQL Server | 2019 / LocalDB / SQL Server Express |
| Visual Studio / Rider / VS Code | Opsional |

---

## 1. Clone & Masuk ke Direktori

```bash
git clone <url-repo> LightenUp
cd LightenUp
```

---

## 2. Konfigurasi Rahasia Lokal

Buat file **`appsettings.Local.json`** di root proyek (file ini di-gitignore):
contoh isi json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=LightenUpDb;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Authentication": {
    "Google": {
      "ClientId": "ISIAN_CLIENT_ID_GOOGLE",
      "ClientSecret": "ISIAN_CLIENT_SECRET_GOOGLE"
    }
  },
  "Duitku": {
    "MerchantCode": "KODE_MERCHANT_DUITKU",
    "ApiKey": "API_KEY_DUITKU",
    "BaseUrl": "https://sandbox.duitku.com/webapi/api/merchant"
  },
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "SenderEmail": "email@gmail.com",
    "SenderPassword": "APP_PASSWORD_GMAIL"
  },
  "AdminSeed": {
    "Password": "Password_Admin_Kuat!"
  },
  "DevMode": {
    "LocalOtp": true
  }
}
```

### Penjelasan `DevMode`

`DevMode.LocalOtp` digunakan **hanya saat development lokal** untuk bypass pengiriman email OTP:

- **`true`** → OTP tidak dikirim via email. Saat register, masukkan kode **`1234`** di kolom OTP — selalu berhasil tanpa perlu konfigurasi SMTP
- **`false` atau tidak ada bagian `DevMode`** → OTP dikirim via email SMTP seperti biasa (mode production)

> **Wajib hapus seluruh bagian `DevMode` dari konfigurasi production** agar OTP dikirim via email sungguhan.

---

## 3. Cara Mendapatkan Setiap API

### Google OAuth (Login dengan Google)

1. Buka [Google Cloud Console](https://console.cloud.google.com/)
2. Buat project baru (atau pilih yang sudah ada)
3. **APIs & Services → Credentials → Create Credentials → OAuth 2.0 Client ID**
4. Application type: **Web application**
5. Tambahkan Authorized redirect URIs:
   - `https://localhost:7040/signin-google` (development)
   - `https://domain-produksi.com/signin-google` (production)
6. Salin **Client ID** dan **Client Secret** → isi ke `appsettings.Local.json`

### Duitku Payment Gateway

1. Daftar di [duitku.com](https://duitku.com) sebagai merchant
2. Dashboard → **Project** → buat project baru
3. Salin **Merchant Code** dan **API Key**
4. Untuk development: gunakan URL sandbox `https://sandbox.duitku.com/webapi/api/merchant`
5. Untuk production: ganti ke `https://passport.duitku.com/webapi/api/merchant`
6. Daftarkan **Callback URL** di dashboard Duitku:
   - `https://domain-produksi.com/Patient/Subscription/Return`

### Gmail SMTP (Kirim Email OTP ke User)

1. Aktifkan **2-Factor Authentication** di akun Google pengirim
2. Buka [myaccount.google.com/apppasswords](https://myaccount.google.com/apppasswords)
3. Buat App Password baru → pilih **Mail** → **Other**
4. Salin 16-karakter password → isi ke `SenderPassword`

---

## 4. Jalankan Migrasi Database

```bash
dotnet ef database update
```

Ini akan membuat database dan menjalankan semua migrasi. Seeding otomatis membuat **satu akun Admin** menggunakan password dari `AdminSeed.Password`.

---

## 5. Jalankan Aplikasi

```bash
dotnet run
```

Atau dengan hot-reload untuk development:

```bash
dotnet watch run
```

Akses di browser: `https://localhost:7040`

---

## Struktur Folder Penting

```
LightenUp/
├── Areas/
│   ├── Admin/          # Dashboard admin
│   ├── HR/             # Dashboard HR perusahaan
│   ├── Patient/        # Dashboard pasien (B2C & Mitra)
│   └── Psychologist/   # Dashboard psikolog
├── Controllers/        # Controller global (Account, dll)
├── Data/               # DbContext & konfigurasi EF Core
├── Migrations/         # Migrasi database
├── Models/             # Entity & ViewModel
├── Services/           # Layanan bisnis (payment, email, dll)
├── wwwroot/            # Asset statis (CSS, JS, gambar)
│   └── uploads/        # Upload pengguna (di-gitignore)
├── appsettings.json            # Konfigurasi umum (aman di-commit)
├── appsettings.Local.json      # Rahasia lokal (DI-GITIGNORE)
└── Program.cs                  # Entry point & DI container
```
