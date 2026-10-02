# LSL Marker Sender

LSL (Lab Streaming Layer) üzerinden marker gönderebilen, profil tabanlı bir Windows masaüstü uygulamasıdır.

## ✨ Özellikler

| Özellik | Açıklama |
|---------|----------|
| **LSL Stream** | Marker tipinde LSL stream oluşturma ve yönetme |
| **Marker Tanımlama** | İstediğiniz kadar marker tanımlayıp adlandırma |
| **Klavye Kısayolları** | Her marker'a isteğe bağlı klavye tuşu atama (global hook) |
| **Ekrandan Butonla Tetikleme** | Dokunmatik veya fare ile ekrandaki renkli butonlara basarak anında marker gönderme |
| **Tablodan Doğrudan Tetikleme** | Marker listesindeki `▶ Gönder` butonuyla tek tıkla test etme ve tetikleme |
| **Büyük Buton (Dokunmatik) Modu** | Dokunmatik ekranlar veya geniş dokunma alanı için buton boyutunu büyütme |
| **Otomatik Stream Başlatma** | Butona tıklandığında stream kapalıysa otomatik olarak başlatma opsiyonu |
| **Anlık Marker Gönderici** | Tanımlı olmayan herhangi bir marker değerini anında yazıp gönderebilme |
| **Gömülü lsl.dll (Tek EXE)** | `lsl.dll` doğrudan EXE içine gömülüdür, ayrı dosya gerekmez |
| **Özel Uygulama İkonu** | Bilimsel EEG dalgası ve marker bayrağı temalı yüksek çözünürlüklü modern ikon |
| **Profil Sistemi** | Marker konfigürasyonlarını JSON profil olarak kaydetme/yükleme |
| **Koyu Tema** | Modern, laboratuvar ortamına uygun karanlık arayüz |

## 🚀 Çalıştırma

### Hazır EXE (Publish edilmiş)

```
publish/
└── LSLMarkerSender.exe   ← Tek başına çalışabilen portable EXE (lsl.dll dahildir!)
```

> [!NOTE]
> `lsl.dll` kütüphanesi doğrudan EXE içerisine gömülmüştür. Başka hiçbir dosya veya .NET kurulumu gerektirmeden tek dosya olarak dilediğiniz bilgisayarda çalıştırabilirsiniz.

### Kaynak koddan derleme

```powershell
# Build
dotnet build -c Release

# Self-contained tek dosya EXE publish
dotnet publish -c Release -o publish/
```

## 📁 Proje Yapısı

```
LSLMarkerSender/
├── LSLMarkerSender.csproj   # Proje dosyası (.NET 8 WinForms)
├── Program.cs               # Giriş noktası
├── MainForm.cs              # Ana form (UI + LSL + keyboard)
├── MarkerDefinition.cs      # Marker veri modeli
├── ProfileManager.cs        # Profil kaydetme/yükleme
├── ShortcutCaptureForm.cs   # Tuş atama dialog penceresi
├── GlobalKeyboardHook.cs    # Win32 global klavye hook
├── LSL.cs                   # Resmi liblsl C# wrapper
├── lib/
│   └── lsl.dll              # Native liblsl kütüphanesi (v1.17.7 x64)
└── publish/
    ├── LSLMarkerSender.exe  # Self-contained tek dosya EXE
    └── lsl.dll              # Native kütüphane
```

## 🔧 Kullanım

1. **Stream Ayarları**: Stream adı, tipi ve kaynak ID'sini ayarlayın
2. **Marker Tanımlama**: DataGridView üzerinden marker ekleyin, değerlerini girin
3. **Tuş Atama**: "Klavye Tuşu" sütununa tıklayıp istediğiniz tuşa basın
4. **Renk Seçme**: "Renk" sütununa tıklayıp düğme rengini seçin
5. **Stream Başlat**: "STREAM BAŞLAT" butonuna tıklayın
6. **Marker Gönder**: Tuşlara basarak veya düğmelere tıklayarak marker gönderin
7. **Profil Kaydet**: "Kaydet" ile ayarları JSON dosyasına kaydedin

## 📋 Varsayılan Marker'lar

| Marker | Tuş | Renk |
|--------|------|------|
| stimulus_start | F1 | 🟢 Yeşil |
| stimulus_end | F2 | 🔴 Kırmızı |
| response | Space | 🔵 Mavi |
| block_start | F5 | 🟠 Turuncu |
| block_end | F6 | 🟣 Mor |

## 🛠 Teknik Detaylar

- **.NET 8.0** (Windows Forms)
- **liblsl v1.17.7** (native C kütüphanesi, x64)
- **Newtonsoft.Json** (profil serializasyonu)
- **Win32 Low-Level Keyboard Hook** (global tuş yakalama)
- **Self-contained publish** (hedef bilgisayarda .NET kurulu olması gerekmez)
