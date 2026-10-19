# Ghost, Boza ve evde yön animasyonları

Üretim: yerleşik image_gen aracı, şeffaf PNG; harici API veya CLI kullanılmadı. Kullanıcı fotoğrafları yalnız karakter referansıydı, depoya eklenmedi.

Ghost: tasmasız, tamamen beyaz uzun tüylü Van kedisi, mavi ve kehribar-yeşil göz. Boza: referans fotoğraflardaki beyaz göğüslü, turuncu-kahverengi tekir desenli, yeşil gözlü kedi.

Son varlıklar:
- Assets/_Game/Art/AnimalsRealistic/Cats/ghost/{walk,home,action,care,climb}.png
- Assets/_Game/Art/AnimalsRealistic/Cats/boza/{walk,home,action,care,climb}.png
- Assets/_Game/Art/AnimalsRealistic/HomeDirections/{silver,calico,greywhite,black,ghost,boza}.png

Atlaslar Unity Sprite Multiple metadatasıyla dilimlenir; kaynak PNG pikselleri değiştirilmez. Boza action atlasının satırları sırasıyla 4, 5 ve 3 poz içerir. Duruşlar ayrı animasyon durumlarına bağlanır. Önden/arkadan hareket sadece evde kullanılır.

## Karakter istemleri

### Ghost
Production 2D realistic Unity game sprite atlas, transparent alpha background. Ghost is a completely SNOW WHITE fluffy long-haired adult Turkish Van cat with a bushy WHITE tail, pink nose and inner ears. Heterochromia: one blue eye, one amber-green eye. NO collar, NO tags, NO accessories, NO colored fur patches. Reference photos establish breed/fur/eyes only, REMOVE the collar and orange markings. Realistic natural feline anatomy, matching a realistic side-scrolling cat game, not cartoon. Every sprite is separate with ample transparent gutters, same body scale and same paw baseline within each equal cell. Full body including tail and ears must fit in each cell. No shadows, floor, text, numbering or grid. All side-view sprites face RIGHT. 

### Boza
Production realistic 2D Unity game sprite atlas on genuine transparent alpha background. Character BOZA: adult shorthaired torbie-and-white domestic cat from reference PHOTOS. Stocky healthy build, green eyes, pink-brown nose with dark outline, broad white blaze from forehead between eyes down nose, white muzzle/chin/chest/belly and white paws. Brown-black tabby striped forehead crown and back, warm orange patches on cheeks/shoulders/flanks mixed with brown dark tabby patches, striped brown tail. NOT a generic black-orange calico: preserve reference warm brown tabby striping with orange. NO collar or accessories. All four paws, ears and tail fully in frame with generous transparent margins and gutters. Constant torso scale, same paw ground baseline per cell, natural anatomy, photorealistic fur, no cartoon. No floor/shadow/text/grid/numbers. All side poses face RIGHT. Photos are character reference only; no bed, house, other cat or room. 

## Animasyon istemleri

### Home
Exactly 4 columns by 3 rows,12 sprites. Read left-to-right top-to-bottom. Cells1-4 standing idle with subtle breathing, looking right, same torso size as walking reference. Cells5-6 sitting upright, cell7 lying awake low to floor, cells8-12 curled asleep with head tucked and eyes closed. Natural different transition/sleep poses, all facing RIGHT and constant torso length for standing, same cat for all poses.

### Action
Exactly 4 columns by 3 rows,12 sprites. Cells1-4 consecutive phases of a jump to the RIGHT: takeoff, rising with forelegs reaching, stretched flight, landing crouch. Cell5 compact airborne apex with paws tucked. Cells6-9 four consecutive crouch-walk cycle poses moving right, belly very low, elbows and knees bent. Cell10 static crouching low. Cells11-12 upright sitting portrait three-quarter face toward viewer so both DIFFERENT colored eyes visible (one BLUE one AMBER GREEN). No object or environment.

### Care
Exactly 4 columns by 3 rows,12 sprites. Cells1-4 eating from an imaginary floor bowl, head down and muzzle near paws (NO bowl drawn). Cells5-8 drinking from imaginary floor bowl, head low and tongue lapping (NO bowl drawn). Cells9-12 standing idle gentle breathing, head turned slightly toward viewer so both different eyes can be seen. Head-to-floor height appropriate, body length same as walking reference.

### Climb
Exactly 4 columns by 3 rows,12 sprites. Twelve consecutive different climbing animation frames. Cat body diagonal steeply upright head toward upper RIGHT, all four paws alternating reaching upward and gripping imaginary tree. NO tree, no surface, no objects. Full fluffy tail behind lower body. Natural realistic climbing cycle, not jumping or horizontal walking.

Boza için aynı düzen kullanıldı; göz renkleri yeşil olarak değiştirildi. Yürüme: 4 sütun × 3 satır, 12 ardışık doğal yürüyüş karesi, sağ profil, sabit beden ölçeği. Ev yönleri: 6 sütun × 2 satır, üstte kameraya doğru 6 yürüyüş karesi, altta kameradan uzağa 6 kare, iki göz ve yüz önden görünür; Ghost için izleyenin sol gözü kehribar-yeşil, sağ gözü mavi. Ortak kısıtlar: gerçek şeffaf alfa, ayrı tam beden pozları, aynı beden ölçeği ve pati tabanı, metin/ızgara/gölge/aksesuar yok.

## Mevcut kedilerin ev yönleri

Use the input image ONLY as reference for the identity and coat pattern of this existing realistic game cat. Create a production 2D sprite animation ATLAS: exactly 6 COLUMNS by 2 ROWS of 12 equally spaced separate full-body cat sprites, landscape canvas. TOP ROW: six frames of a smooth walking cycle directly TOWARD the camera/viewer (FRONT view), face and both eyes visible, four paws alternate in a natural slow feline walk, tail visible behind. BOTTOM ROW: six frames of walking directly AWAY from the camera (REAR view), back of head, ears, shoulders, hindquarters, ringed tail visible; no face in the back row. Same adult cat, not a kitten, not chibi or cartoon. Photorealistic soft fur and natural proportions matching reference image. NO side views, no three-quarter profiles, the body axis points straight toward or straight away, not left or right. Genuine transparent alpha background, no ground, no shadows, no lettering, no numbering, no grid lines, no collars or clothing or accessories. Every full cat including ears tail and paws must fit in its cell with generous transparent gutters. Common shoulder/torso scale and paw ground baseline within each cell for a smooth loop. Alternating front/rear paw strides distinctly different across six frames, body subtly rises/falls but never changes body size. The front walk should show chest, muzzle, whiskers and eyes; the rear walk should show shoulder blades shifting and alternating hind paws. Keep eyes the same color as input and markings consistent across every frame. 

Renk eşlemeleri: Mitzi silver, Tarçın calico, Pamuk/Ada greywhite (siyah-beyaz), Kömür/Moka black. Her atlas kendi mevcut yan görünümünden referanslandı. Ghost ve Boza kendilerine ait atlas kullanır.

