# Blazma

<img src="assets/blazma-preview.png" width="96" align="right" alt="Blazma" />

تطبيق Crosshair Overlay خفيف لـ Windows 10 و Windows 11، مبني من جزأين:

- **محرك الرسم** (C++ / Win32 + GDI+) — نافذة شفافة دائمًا في المقدمة، تمرّ منها نقرات الماوس.
- **واجهة الإعدادات** (C# / Avalonia UI) — واجهة داكنة حديثة مع معاينة حية، بأربع لغات.

الجزءان يتشاركان ملف `config.ini` فقط، فيبقى محرك الرسم خفيفًا ومستقلاً تمامًا.

## هيكل المشروع

```
Blazma/
├── build.ps1                    يبني الجزأين وينتج dist/
├── build-mingw.ps1              يبني محرك الرسم وحده
├── config.ini                   إعدادات البداية (تُنسخ للوضع المحمول)
├── Blazma.sln / Blazma.vcxproj  مشروع Visual Studio لمحرك الرسم
├── assets/
│   └── blazma.ico               أيقونة التطبيق (تُدمج في الملفين)
│
├── src/                         محرك الرسم (C++)
│   ├── Config.h / .cpp          قراءة وكتابة config.ini وتحديد مساره
│   ├── CrosshairRenderer.h/.cpp الرسم بـ GDI+
│   ├── Ipc.h                    عقد الاتصال مع الواجهة
│   ├── Blazma.rc / .manifest    الأيقونة وبيانات الإصدار و DPI
│   └── main.cpp                 النافذة والاختصارات ودورة الرسائل
│
└── ui/                          واجهة الإعدادات (Avalonia)
    ├── Localization/            Language.cs, Strings.cs (4 لغات)
    ├── Models/                  CrosshairConfig, CrosshairPreset
    ├── Services/                ConfigService, OverlayService
    ├── Controls/                CrosshairPreview, CalibrationField
    ├── ViewModels/              MainViewModel, RelayCommand
    ├── Views/                   MainWindow
    └── Styles/                  Tokens.axaml (الألوان والخطوط), Theme.axaml
```

## البناء

```powershell
.\build.ps1
```

يُنتج مجلد `dist/` جاهزًا للتشغيل:

| الملف | الحجم | ملاحظات |
|---|---|---|
| `Blazma.exe` | ~48 MB | واجهة الإعدادات — هذا ما تشغّله |
| `BlazmaOverlay.exe` | ~1.4 MB | محرك الرسم، بدون أي اعتماديات خارجية |

المتطلبات لأول بناء فقط:

```powershell
winget install --id MartinStorsjo.LLVM-MinGW.UCRT
winget install --id Microsoft.DotNet.SDK.10
```

`.\build.ps1 -CoreOnly` يبني محرك الرسم وحده · `.\build.ps1 -Portable` ينسخ `config.ini` إلى `dist/`.

## أين تُحفظ الإعدادات

| الحالة | المسار |
|---|---|
| مُثبَّت (ستيم، Program Files) | `%APPDATA%\Blazma\config.ini` |
| محمول | `config.ini` بجانب الملف التنفيذي — إن وُجد، له الأولوية |

هذا مقصود: مجلدات التثبيت غير قابلة للكتابة، فلو بقيت الإعدادات بجانب الملف التنفيذي لفشل الحفظ عند التثبيت عبر ستيم.

## طريقة الاستخدام

شغّل `Blazma.exe`، ثم اضغط **Start overlay**. كل تعديل يُحفظ ويُطبَّق فورًا على الـ overlay أثناء عمله.

### الاختصارات (تعمل داخل الألعاب)

| الاختصار | الوظيفة |
|---|---|
| `Ctrl + Shift + C` | إظهار / إخفاء الـ Crosshair |
| `Ctrl + Shift + M` | فتح نافذة الإعدادات |
| `Ctrl + Shift + S` | فتح `config.ini` في Notepad |
| `Ctrl + Shift + Q` | إغلاق الـ overlay |

> كل الاختصارات تستخدم `Ctrl + Shift` عمدًا. الاختصار بمفتاح واحد (مثل `Insert` أو `End`) يحجز المفتاح على مستوى النظام بالكامل، فلا يصل إلى البرنامج الذي تكتب فيه، وأي ضغطة عابرة تغلق التطبيق.

## الإعدادات

| المفتاح | الوصف | القيم |
|---|---|---|
| `type` | نوع الشكل | `cross`, `dot`, `circle`, `cross_dot`, `circle_dot` |
| `color` | اللون | `0xRRGGBB` أو `#RRGGBB` |
| `size` | نصف القطر أو نصف طول الخط | 1–500 |
| `thickness` | سمك الخط | 1–100 |
| `gap` | الفراغ في المنتصف | 0–50 |
| `outline` | حدود داكنة للتباين | `0` أو `1` |
| `opacity` | الشفافية | 0–255 |
| `language` | لغة الواجهة | `en`, `ru`, `ar`, `zh` |

ستة أنماط جاهزة: Classic، Precision، Micro Dot، Ring، Sniper، Neon.

## اللغات

تُبدَّل فورًا بدون إعادة تشغيل من قسم **اللغة**:

| اللغة | المفتاح | الاتجاه | الخط |
|---|---|---|---|
| English | `en` | LTR | Bahnschrift |
| Русский | `ru` | LTR | Bahnschrift (يغطي السيريلية) |
| العربية | `ar` | **RTL** — النافذة كاملة تنعكس، بما فيها شريط العنوان | Segoe UI |
| 中文 | `zh` | LTR | Microsoft YaHei |

الخطوط تتبع اللغة لأن Bahnschrift لا يحتوي على حروف عربية ولا صينية. الأرقام تبقى بخط Cascadia Mono في كل اللغات حتى لا ترتجف أثناء سحب المؤشرات.

مفتاح `language` يُخزَّن في نفس `config.ini`، ومحرك الرسم يتجاهله لأنه يتخطى أي مفتاح لا يعرفه.

## كيف تتصل الواجهة بمحرك الرسم

الواجهة تكتب `config.ini` ثم ترسل رسالة نافذة مسجّلة إلى محرك الرسم، فيعيد قراءة الملف ويرسم فورًا. العقد معرّف في [src/Ipc.h](src/Ipc.h) و [ui/Services/OverlayService.cs](ui/Services/OverlayService.cs)، ويجب أن يبقى الطرفان متطابقين:

```
class   : BlazmaOverlay
message : Blazma.Command
wParam  : 0=Reload  1=Show  2=Hide  3=Toggle  4=Exit
```

## الشاشات المتعددة وتكبير الشاشة (DPI)

- محرك الرسم يعمل بـ **Per-Monitor DPI Awareness**، فيُرسم الـ Crosshair بدقة البكسل الحقيقية بدون تمدد أو ضبابية على الشاشات المكبَّرة.
- التوسيط على **الشاشة التي بها مؤشر الماوس** — ضع المؤشر على شاشة اللعبة ثم اضغط Restart.
- عند تغيّر دقة الشاشة (مثلًا عند تشغيل لعبة) يُعاد التوسيط تلقائيًا.
- لا يمكن تشغيل أكثر من نسخة واحدة من محرك الرسم في نفس الوقت.

## سلامة الألعاب

- `WS_EX_NOACTIVATE` لا يسرق التركيز من اللعبة.
- `WS_EX_TRANSPARENT` يمرّر نقرات الماوس إلى ما تحته.
- `WS_EX_TOOLWINDOW` يخفيه من شريط المهام و Alt+Tab.
- لا يقوم بحقن أي DLL ولا بتعديل ذاكرة الألعاب.
- **تحذير**: الألعاب في وضع **Fullscreen Exclusive** قد تخفي أي نافذة أخرى — استخدم **Borderless Windowed**.
