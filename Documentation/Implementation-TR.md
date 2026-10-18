# Yeni arayüz ve ödül akışı

Bu değişikliklerde oyun kuralları ile görsel sunum ayrı tutuldu. Banka yeni bir kalıcı para sistemi değil; mevcut oturumda kazanılan ödüllerin görsel listesidir. Uygulama kapanınca oturum sıfırlanır.

## Çarklar ve ödül seçimi

`WheelDefinition` her çarkın görsellerini, sekiz temel dilimini ve isteğe bağlı ödül havuzunu tutar. Havuz boşsa sekiz dilim aynen oynanır. Havuz doluysa `WheelRewardSelector.Select` havuzdan farklı ödüller seçer, iki para türünü korur ve normal çarka tek bomba ekler. Gümüş/altın çarkın temel dilimlerinde bomba bulunmaz.

Seçim her tıklamada yapılmaz. `GameSessionController.PrepareLayouts` mevcut ve sonraki bölgenin içeriklerini bir kez oluşturur. `RequestSpin` yalnızca mevcut sekiz dilimden bir sonuç seçer ve animasyonu başlatır. Böylece oyuncunun gördüğü içerik, butona bastığında değişmez.

Gümüş, daha yüksek para/eşya miktarlarına ve daha geniş eşya havuzuna sahiptir. Altında normal sarf malzemeleri ve geliştirme puanları bulunmaz; elit silahlar, altın/süper sandıklar ve özel ekipmanlar vardır. Tüm miktarlar wheel asset'lerindeki Reward Pool alanından değiştirilebilir. Altın ve para miktarları ayrıca mevcut bölge artışını kullanır; eşyalar kendi adetlerini korur.

## Sonuç penceresi ve bölge geçişi

`HandleSpinCompleted` kazancı `GameSession.ApplySpin` ile bankaya ekler ve `IsRewardPending` durumunu açar. Bu sırada çark kazanan dilimde durur ve yeni çevirme/çıkış işlemleri kabul edilmez.

`RewardRevealView.Show` kazanan görseli, adını ve gerçek kazanılan miktarı gösterir. Star flash yavaşça döner, star glow hafifçe büyüyüp küçülür. Bu animasyonlar açılır pencerenin alt nesnelerinde çalışır. Bomba sonucunda kırmızı sunum kullanılır ve banka boşalır.

Ekrana tek dokunuş `Dismiss` fonksiyonunu çağırır. Bu fonksiyon pencereyi kapatır ve `AcknowledgeReward` ile oyuna devam eder. Başarılı sonuçta `IsTransitioning` açılır ve `WheelTransitionView.Play` geçişi başlatır. Mevcut çark sola küçülerek kayarken sağdaki önizleme merkeze büyüyerek gelir. Hareket 0,58 saniye sürer ve InOutCubic ile başta/sonda yavaşlar. Butonun kökü yerinde kalır; yalnızca görünürlüğü yumuşakça azalır/artar.

Önizleme merkeze ulaşınca `CommitZoneTransition` yeni bölgenin içeriğini bir kez devreye alır. Aynı boyut ve konumda görsel değişimi yapıldığı için çark atlamaz. Yeni çarkın açısı sıfırlanır; soldaki önceki çark bitirdiği açıyı korur. Son 0,16 saniyede yeni sağ önizleme, merkez butonu ve miktarlar belirir. `CompleteZoneTransition` giriş kilidini ancak bu aşama tamamlanınca kaldırır. Animasyon alt nesnelerde çalışır; sabit yerleşim kökleri taşınmaz.

Geçiş sırasında çevirme, banka, çıkış ve yeniden başlatma istekleri reddedilir. Tekrarlanan kapatma çağrısı bölge atlatamaz. Animasyon bileşeni devre dışı kalırsa geçiş güvenle tamamlanır ve görsel konumlar geri yüklenir; oyun kilitli kalmaz.

## Banka

`BankView.Open` oturumun Rewards sözlüğünü okur. Her farklı ödül için bir `BankRewardRowView` oluşturur veya önceden oluşturduğu satırı tekrar kullanır. Satırda ikon, isim ve miktar bulunur. Aynı ödül tekrar kazanılırsa mevcut kaydın miktarı artar.

Liste ScrollRect ile kaydırılır. Maskeleme yalnızca kaydırılan satırlarda açıktır; diğer dekoratif görseller tıklama yakalamaz. Banka açılınca controller girişleri kilitler, `Close` kapatınca kilidi kaldırır. Ödülleri almak bankaya bakmaktan ayrı bir işlemdir: yalnızca güvenli/süper bölgede, çark dururken yapılabilir.

## Arayüzün sorumlulukları

- `WheelHudView`: aktif bölge, çark türü, kısa yönlendirme, banka sayacı ve buton görünürlüğü.
- `WheelPreviewView`: bir çarkın ve sekiz ödülünün küçük önizlemesi; hareket eden görselleri sabit bölge etiketinden ayrı tutar.
- `WheelTransitionView`: çarklar arasındaki kayma, ölçek ve görünürlük animasyonlarını yönetir; oyun kuralları veya ödül seçimi yapmaz.
- `WheelContentView`: aktif çarkın ikon/miktarlarını doldurur ve dönüş sırasında okunabilir tutar.
- `WheelSpinView`: yalnızca verilen dilime dönüş animasyonu yapar; ödül seçmez.
- `WheelBackdropGraphic`: çözünürlükten bağımsız koyu gradyan, ince çizgiler ve halkalar çizer.
- `ButtonPressFeedback`: butonun altındaki görseli hafifçe küçültür; kök yerleşimini değiştirmez.

Merkezdeki SPIN butonu dönen çarkın çocuğu değildir. İkisi Canvas altında aynı konumda duran kardeş nesnelerdir. Bu yüzden çark dönerken buton sabit kalır.

## Editor araçları ve doğrulama

`WheelPresentationBuilder` normal düzenlenebilir prefab'ı ve ödül asset'lerini hazırlayan Editor aracıdır. Oyun açılırken arayüzü kodla yeniden üretmez. Gündelik düzenlemeler prefab üzerinden yapılabilir. **Apply Presentation** mevcut görsel yerleşimi/havuzları yeniden kurduğu için elle yapılan tasarım değişikliklerini sıfırlar; normal çalıştırma için bu komuta basılmaz.

`WheelPresentationChecks` yalnızca Editor içinde çalışır. Belirli bölgeleri ve sonuçları kontrollü olarak hazırlayıp gerçek butonların olaylarını ve gerçek oyun bileşenlerini test eder. Üretim koduna hile menüsü eklemez. Test sırasında dönüş süresi geçici olarak kısaltılır ve Play modu kapanınca normal değer korunur. Unity kamera çıktılarından farklı oranlarda görseller kaydedilir. Bu kontroller son APK'nın Android'de denenmesinin yerine geçmez.
