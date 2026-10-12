**🌐 Language / Dil:** [English](README.md) | Türkçe

# E-Dönüşüm Demo — E-Fatura, E-Arşiv Fatura ve E-İrsaliye (UBL-TR XML) Üretici

![C#](https://img.shields.io/badge/C%23-239120?logo=csharp&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet&logoColor=white)
![WPF](https://img.shields.io/badge/WPF-0078D6?logo=windows&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL%20Server-CC2927?logo=microsoftsqlserver&logoColor=white)
![UBL](https://img.shields.io/badge/UBL-2.1%20(TR1.2)-orange)
![Platform](https://img.shields.io/badge/Platform-Windows-lightgrey)

**E-Dönüşüm Demo**, Türkiye'deki elektronik belge sürecini (e-dönüşüm) göstermek için hazırlanmış bir Windows masaüstü uygulamasıdır. Cari ve stok kartlarını yönetir; **E-Fatura**, **E-Arşiv Fatura** ve **E-İrsaliye** oluşturur, belgeleri SQL Server'da saklar ve her belge için **UBL-TR yapısında bir XML dosyası** üretir.

Proje, Hetabil Yazılım stajı sürecinde geliştirilmiştir.

> **Önemli:** Bu proje bir demodur. Üretilen XML, UBL 2.1 / TR1.2 yapısını sadeleştirilmiş biçimde izler. Proje belgeleri **dijital olarak imzalamaz**, Gelir İdaresi Başkanlığı'na (GİB) veya bir entegratöre **hiçbir şey göndermez** ve XML'i resmî şemalara göre **doğrulamaz**. Çıktıyı hukuken geçerli bir e-belge olarak kullanmayın. Bkz. [Bilinen sınırlamalar](#10-bilinen-sınırlamalar).

---

## İçindekiler

1. [Özellikler](#1-özellikler)
2. [Nasıl çalışır?](#2-nasıl-çalışır)
3. [Beş sekme](#3-beş-sekme)
4. [Üretilen XML](#4-üretilen-xml)
5. [Doğrulama ve hesaplama kuralları](#5-doğrulama-ve-hesaplama-kuralları)
6. [Kullanılan teknolojiler](#6-kullanılan-teknolojiler)
7. [Proje yapısı](#7-proje-yapısı)
8. [Veritabanı](#8-veritabanı)
9. [Kurulum ve çalıştırma](#9-kurulum-ve-çalıştırma)
10. [Bilinen sınırlamalar](#10-bilinen-sınırlamalar)

---

## 1. Özellikler

- **Cari kartlar:** Unvan, vergi dairesi, vergi no (VKN/TCKN), adres, telefon, e-posta ve e-Fatura işaretiyle müşteri veya tedarikçi kaydı oluşturma ve düzenleme.
- **Stok / malzeme:** Kod, ad, birim, KDV oranı ve birim fiyatıyla ürün ve hizmet kayıtları.
- **Fatura:** Kalemlerden fatura hazırlama, **E-Fatura** veya **E-Arşiv Fatura** seçimi; ara toplam, KDV toplamı ve genel toplam anlık güncellenir.
- **İrsaliye:** Gönderen, alıcı, sevk tarihi, taşıyıcı bilgisi ve kalemlerle **E-İrsaliye** hazırlama.
- **UBL-TR XML çıktısı:** Kaydedilen her belge için bir XML dosyası üretilir ve uygulama içinde önizlenebilir.
- **Belge listesi:** Tüm belgeler tek ekranda; tarih aralığı, cari ve belge tipine göre filtreleme.
- **Düzenleme ve silme:** Kayıtlı belgeyi tekrar açıp düzenleme veya XML dosyasıyla birlikte silme.
- **SQL Server saklama:** Veritabanı ve tablolar ilk çalıştırmada otomatik oluşturulur, örnek veriler eklenir.
- **Girdi kontrolleri:** Vergi no, telefon ve e-posta Türkiye biçimine göre kaydetmeden önce denetlenir.

---

## 2. Nasıl çalışır?

1. Uygulama açılırken SQL Server bağlantı cümlesini bir metin dosyasından okur (dosya yoksa varsayılanla oluşturur).
2. `EDonusumDemo` veritabanı ve tablolar yoksa oluşturulur; boş veritabanına örnek cari ve ürünler yüklenir.
3. Sekmelerden cari, ürün, fatura ve irsaliye oluşturursunuz.
4. Fatura veya irsaliyede **Kaydet**'e bastığınızda:
   1. Belge doğrulanır.
   2. `UblXmlGenerator` UBL XML'ini oluşturur.
   3. XML, `XmlOrnekleri` klasörüne yazılır.
   4. Belge, kalemleri ve XML yolu SQL Server'a kaydedilir.
   5. Form, sıradaki belge için temizlenir.

Belge numaraları otomatik önerilir: faturalar için `FTR20261012-001`, irsaliyeler için `IRS20261012-001` (ön ek + tarih + sıra sayısı). Numarayı değiştirebilirsiniz.

---

## 3. Beş sekme

Pencere başlığı **E-Dönüşüm Demo**'dur. Arayüz Türkçedir.

| Sekme | Amaç | Ana kontroller |
|-------|------|----------------|
| **Cari Kartlar** | Müşteri ve tedarikçiler | Liste, form (*Cari Unvan, Vergi Dairesi, Vergi No / TCKN, Adres, Telefon, E-posta*, e-Fatura kutusu), **Yeni**, **Kaydet** |
| **Stok / Malzeme** | Ürün ve hizmetler | Liste, form (*Ürün Kodu, Ürün Adı, Birim, KDV Oranı, Birim Fiyat*), **Yeni**, **Kaydet** |
| **Fatura** | Fatura oluşturma veya düzenleme | *Fatura No, Fatura Tarihi, Satıcı, Alıcı, Belge Tipi, Açıklama*, kalem girişi (**Kalem Ekle**, **Seçileni Sil**), toplamlar, XML önizleme, **Kaydet** |
| **İrsaliye** | İrsaliye oluşturma veya düzenleme | *İrsaliye No, Tarih, Sevk Tarihi, Gönderen, Alıcı, Taşıyıcı Bilgisi*, kalem girişi, XML önizleme, **Kaydet** |
| **Belgeler** | Tüm belgeleri listeleme | Filtreler (*Başlangıç Tarihi, Bitiş Tarihi, Cari, Belge Tipi*), **Filtrele**, **Temizle**, **Belgeyi Aç**, **Sil**, **XML Göster** |

**Ürün birimleri:** `ADET`, `SAAT`, `GÜN`, `KG`, `LT`, `METRE`, `PAKET`.

**Filtredeki belge tipleri:** Tümü, E-Fatura, E-Arşiv Fatura, E-İrsaliye.

---

## 4. Üretilen XML

| Belge | Kök öğe | `ProfileID` |
|-------|---------|-------------|
| E-Fatura | `Invoice` (UBL Invoice-2) | `TEMELFATURA` |
| E-Arşiv Fatura | `Invoice` (UBL Invoice-2) | `EARSIVFATURA` |
| E-İrsaliye | `DespatchAdvice` (UBL DespatchAdvice-2) | `TEMELIRSALIYE` |

Üreticinin kullandığı sabit değerler:

| Öğe | Değer |
|-----|-------|
| `UBLVersionID` | `2.1` |
| `CustomizationID` | `TR1.2` |
| `InvoiceTypeCode` | `SATIS` (satış) |
| Para birimi (`currencyID`) | `TRY` |
| KDV vergi şeması | `KDV`, kod `0015` |
| Taraf kimlik şeması | 11 haneli numara için `TCKN`, aksi halde `VKN` |

**Dosya adı:** `{EFatura | EArsivFatura | EIrsaliye}_{numara}_{yyyyMMdd_HHmmss}.xml`, örneğin `EFatura_FTR20260703-007_20260703_121600.xml`.

**Çıktı klasörü:** Proje klasörü içindeki `XmlOrnekleri` (uygulama, çıktı klasöründen yukarı doğru `EDonusumDemo.csproj` dosyasını arar). Proje dosyası bulunamazsa, örneğin yayınlanmış bir kopya çalıştırıldığında, çalıştırılabilir dosyanın yanındaki `XmlOrnekleri` klasörünü kullanır.

Bu klasör yalnızca üretilmiş çıktıları içerdiği için Git'e dahil edilmez (bkz. `.gitignore`).

Hazır örnekler [`Samples`](EDonusumDemo/Samples) klasöründedir: `sample-invoice.xml` ve `sample-waybill.xml`.

---

## 5. Doğrulama ve hesaplama kuralları

### Cari kartı

| Alan | Kural |
|------|-------|
| Unvan | Zorunlu |
| Vergi no | Zorunlu; tam 10 hane (VKN) veya 11 hane (TCKN); yalnızca rakam |
| Telefon | Zorunlu; Türkiye biçimi. `03129111257`, `3129111257` ve `+903129111257` kabul edilir, `0 (312) 911 12 57` olarak saklanır |
| E-posta | Zorunlu; alan adında nokta bulunan geçerli adres; küçük harfle saklanır |

### Ürün

| Alan | Kural |
|------|-------|
| Kod ve ad | Zorunlu |
| KDV oranı | 0 ile 100 arasında sayı |
| Birim fiyat | 0'dan büyük sayı |

### Fatura ve irsaliye

- Belge numarası zorunludur.
- Satıcı/alıcı (fatura) veya gönderen/alıcı (irsaliye) seçilmelidir.
- En az bir kalem eklenmelidir.
- Miktar 0'dan büyük olmalıdır. Ondalıkları Türkçe biçimde, ondalık ayracı olarak **virgülle** yazın (`1250,50`). Nokta binlik ayracı sayıldığı için `1250.50` değeri 125050 olarak okunur.

### Hesaplamalar

| Değer | Formül |
|-------|--------|
| Kalem tutarı | `yuvarla(miktar × birim fiyat, 2)` |
| Kalem KDV'si | `yuvarla(kalem tutarı × KDV oranı / 100, 2)` |
| Kalem toplamı | kalem tutarı + kalem KDV'si |
| Fatura ara toplam / KDV toplamı / genel toplam | Kalem değerlerinin toplamları |

Örnek: 5.000 TL birim fiyatlı 2 adet ve %20 KDV, 10.000,00 kalem tutarı, 2.000,00 KDV ve 12.000,00 toplam verir.

---

## 6. Kullanılan teknolojiler

| Teknoloji | Kullanım amacı |
|-----------|----------------|
| **C# ve .NET 9** (`net9.0-windows`) | Uygulama dili ve çalışma zamanı |
| **WPF** (XAML) | Masaüstü arayüzü |
| **Microsoft.Data.SqlClient 5.2.2** | SQL Server erişimi |
| **SQL Server** (varsayılan olarak Express) | Veri saklama |
| **System.Xml.Linq** (`XDocument`) | UBL XML üretimi |

---

## 7. Proje yapısı

```text
EDonusumDemo/
├── README.md                          # English documentation
├── README.tr.md                       # Türkçe dokümantasyon
├── .gitignore
└── EDonusumDemo/
    ├── EDonusumDemo.csproj
    ├── App.xaml / App.xaml.cs         # Uygulama girişi
    ├── MainWindow.xaml / .xaml.cs     # Beş sekme ve iş mantığı
    ├── AssemblyInfo.cs
    ├── Application/
    │   └── UblXmlGenerator.cs         # Fatura ve irsaliye XML'ini oluşturur
    ├── Domain/                        # Model sınıfları
    │   ├── Customer.cs
    │   ├── Product.cs
    │   ├── InvoiceDocument.cs
    │   ├── WaybillDocument.cs
    │   ├── DocumentLine.cs            # Tutar ve KDV hesaplarıyla belge kalemi
    │   ├── DocumentKind.cs            # EInvoice, EArchiveInvoice, EWaybill
    │   └── DocumentListItem.cs        # Belge listesi satır modeli
    ├── Infrastructure/
    │   ├── DemoDataStore.cs           # SQL Server erişimi, şema, örnek veri, XML kaydı
    │   └── StoreData.cs               # Bellekteki veri kapsayıcısı
    ├── Database/
    │   └── CreateDatabase.sql         # İsteğe bağlı elle veritabanı betiği
    └── Samples/
        ├── sample-invoice.xml
        └── sample-waybill.xml
```

---

## 8. Veritabanı

**Veritabanı adı:** `EDonusumDemo`

| Tablo | İçerik |
|-------|--------|
| `Customers` | Müşteri ve tedarikçiler |
| `Products` | Ürün ve hizmetler (benzersiz `Code`) |
| `Invoices` | Fatura başlıkları (benzersiz `Number` + `DocumentKind`; tür `EInvoice` veya `EArchiveInvoice`) |
| `InvoiceLines` | Fatura kalemleri (fatura silinince birlikte silinir) |
| `Waybills` | İrsaliye başlıkları (benzersiz `Number`) |
| `WaybillLines` | İrsaliye kalemleri (irsaliye silinince birlikte silinir) |

**Otomatik kurulum:** Uygulama ilk çalıştırmada veritabanını ve tüm tabloları kendisi oluşturur; SQL betiğini çalıştırmak **gerekmez**.

**Örnek veri:** Boş veritabanına iki cari (örnek bir satıcı ve "Demo Müşteri A.Ş."; ikisinde de yer tutucu vergi numaraları var) ve üç ürün eklenir: `SRV-001` Yazılım Geliştirme Hizmeti (SAAT, 1.250), `LIS-001` Demo Lisans (ADET, 5.000) ve `DAN-001` Danışmanlık (GÜN, 3.500). Hepsinde KDV %20'dir.

**Bağlantı cümlesi:** Çalıştırılabilir dosyanın yanındaki `Data\connection-string.txt` dosyasında saklanır (örneğin `bin\Debug\net9.0-windows\Data\connection-string.txt`). İlk çalıştırmada şu varsayılanla oluşturulur:

```text
Server=localhost\SQLEXPRESS;Database=EDonusumDemo;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True;
```

Başka bir sunucu kullanmak için bu dosyayı düzenleyin. Örneğin SQL Server LocalDB için:

```text
Server=(localdb)\MSSQLLocalDB;Database=EDonusumDemo;Trusted_Connection=True;
```

> **İsteğe bağlı betik:** `Database/CreateDatabase.sql` aynı şemayı elle oluşturur. Uygulama tabloları oluşturduktan sonra çalıştırmak **güvenli değildir** ("nesne zaten var" hatası verir). Yalnızca boş bir sunucuda kullanın veya hiç kullanmayın.

---

## 9. Kurulum ve çalıştırma

> **Gereksinimler:**
> - Windows 10 veya 11 (WPF yalnızca Windows'ta çalışır)
> - [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
> - `SQLEXPRESS` adlı varsayılan örneğe ve Windows Kimlik Doğrulamasına sahip SQL Server Express veya bağlantı cümlesini yönlendireceğiniz başka bir SQL Server
> - İsteğe bağlı: ".NET masaüstü geliştirme" iş yüküyle Visual Studio 2022 sürüm 17.12 veya üzeri

### Yöntem A — Visual Studio

1. Depoyu klonlayın:
   ```bash
   git clone https://github.com/berkaycinar-dev/EDonusumDemo.git
   ```
2. Visual Studio'da **Dosya → Aç → Proje/Çözüm** seçeneğini kullanın.
3. `EDonusumDemo/EDonusumDemo.csproj` dosyasını seçin. `.sln` dosyası yoktur; proje dosyasını açmak yeterlidir.
4. SQL Server'ın çalıştığından emin olun (aşağıya bakın).
5. **F5**'e basın.

### Yöntem B — Komut satırı

1. Depoyu klonlayıp proje klasörüne girin:
   ```bash
   git clone https://github.com/berkaycinar-dev/EDonusumDemo.git
   cd EDonusumDemo/EDonusumDemo
   ```
2. Paketleri geri yükleyin:
   ```bash
   dotnet restore
   ```
3. Uygulamayı çalıştırın:
   ```bash
   dotnet run
   ```

### SQL Server'ın hazır olduğunu kontrol etme

1. **Hizmetler**'i (`services.msc`) açın ve **SQL Server (SQLEXPRESS)** hizmetinin *Çalışıyor* olduğunu kontrol edin.
2. Başka bir sunucu veya örnek adı kullanıyorsanız, ilk çalıştırmadan sonra `Data\connection-string.txt` dosyasını düzenleyin (uygulama bağlanamazsa dosya yolunu içeren bir hata penceresi gösterir) ve yeniden başlatın.
3. Başarılı başlangıçta durum satırında "SQL Server bağlı" yazısı ve bağlantı dosyasının yolu görünür.

### İlk kullanım

1. **Cari Kartlar** sekmesini açın ve iki örnek carıyı kontrol edin (veya kendi carinizi ekleyin).
2. **Stok / Malzeme** sekmesini açın ve üç örnek ürünü kontrol edin.
3. **Fatura** sekmesinde satıcı, alıcı ve belge tipi seçin, bir ürün seçip **Kalem Ekle**'ye, ardından **Kaydet**'e basın.
4. **Belgeler** sekmesini açın, yeni satırı seçip **XML Göster**'e basarak XML'i görüntüleyin.
5. XML dosyasını `XmlOrnekleri` klasöründe bulun.

---

## 10. Bilinen sınırlamalar

- **Sadeleştirilmiş UBL-TR:** XML dijital olarak imzalanmaz, UUID içermez ve resmî GİB şemalarına ve iş kurallarına göre doğrulanmaz.
- **Entegrasyon yok:** GİB'e veya e-fatura entegratörüne hiçbir şey gönderilmez; proje yalnızca yerel dosya üretir.
- **Tek KDV bloğu:** Kalemlerde farklı KDV oranları olsa bile faturada toplam KDV için tek bir `TaxSubtotal` bulunur. İstisna ve tevkifat desteklenmez.
- **Sabit değerler:** Para birimi her zaman `TRY`, fatura tipi her zaman `SATIS`'tır.
- **E-Fatura kutusu:** Carideki "e-Fatura mükellefi" işareti saklanır ve listede gösterilir, ancak belge üretiminde kullanılmaz.
- **İrsaliye tutarı:** İrsaliyelerde fiyat bulunmadığı için belge listesinde tutar 0 görünür.
- **Eski XML dosyaları kalır:** Düzenlenmiş bir belgeyi kaydetmek zaman damgalı yeni bir XML dosyası yazar; önceki dosya silinmez (yalnızca belge silindiğinde silinir).
- **Cari ve ürün silme yok:** Arayüzden yalnızca belgeler silinebilir.
- **Bütün veriyi kaydetme:** Her kayıtta tüm cari, ürün ve belgeler tek işlemde veritabanına yazılır. Demo için uygundur, büyük veride verimli değildir.
- **Ondalık girişi:** Sayılar önce Türkçe kültürle okunur; bu yüzden nokta binlik ayracı sayılır. Ondalıklar için virgül kullanın.
- **Yalnızca Windows.**
