# Plan inspekcije koda – Task 1.1

**Predmet:** Verifikacija i validacija softvera 2026/27
**Tim:** Grupa1-Tim4
**Kod koji se pregleda:** aplikacija „ESportskiCentar“ (Tim 3), branch `inspekcija-tim3`
**Pull request:** `inspekcija-tim3` → `inspekcija-base`
**Osnova:** Predavanje 2 (inspekcija koda, liste provjere, skala ozbiljnosti) i Predavanje 3 (sigurnosna inspekcija, OWASP Top 10 2025, CWE)

---

## 1. Uloge (Predavanje 2, slajd 34)

| Član | GitHub | Uloga |
|---|---|---|
| Dino Tahirović | `dtahirovic1` | Moderator, zapisničar i inspektor (manji dio koda) |
| Amir Džafić | `amirdzafiic` | Inspektor |
| Imran Čustović | `icustovic1` | Inspektor |

**Autor koda:** Tim 3. Niko iz našeg tima ne pregleda svoj kod.

U manjim timovima moderator preuzima i ulogu zapisničara (P2, slajd 34). Moderator:
- dijeli kod i liste provjere;
- vodi sastanak inspekcije;
- prikuplja nalaze;
- piše sumarni izvještaj (P2, slajd 42).

---

## 2. Podjela koda

Iz pregleda su izostavljeni generisani i vanjski fajlovi (EF migracije, bootstrap/jquery, slike, publish profili), commit `b1e711b`. Ukupno za pregled ostaje **~5.490 linija**.

### Dino Tahirović – ~1.040 linija (~19 %)
Fokus: autentifikacija, konfiguracija i infrastruktura. **Naglasak na CL-S.**

| Fajl | Linija |
|---|---|
| Areas/Identity/Pages/Account/Login.cshtml.cs | 140 |
| Areas/Identity/Pages/Account/Login.cshtml | 70 |
| Areas/Identity/Pages/Account/Register.cshtml.cs | 163 |
| Areas/Identity/Pages/Account/Register.cshtml | 83 |
| Areas/Identity/Pages/Account/AccessDenied.cshtml(.cs) | 38 |
| Areas/Identity/Pages/_ViewImports, _ViewStart, _ValidationScriptsPartial, Account/_ViewImports | 10 |
| Views/Shared/_LoginPartial.cshtml | 26 |
| Program.cs | 62 |
| appsettings.json, appsettings.Development.json | 27 |
| Properties/launchSettings.json | 33 |
| Dockerfile | 28 |
| ESportskiCentar.csproj, ESportskiCentar.slnx, dotnet-tools.json | 41 |
| Data/ApplicationDbContext.cs | 35 |
| Services/EmailService.cs | 49 |
| Helpers/VrijemeHelper.cs | 24 |
| Attributes/SamoBuducnostAttribute.cs, ValidacijaRasponaDvaDatuma.cs | 42 |
| Controllers/HomeController.cs, Models/ErrorViewModel.cs | 29 |
| Views/Home/Index.cshtml | 103 |
| Views/Shared/Error.cshtml, Views/_ViewImports, _ViewStart, Shared/_ValidationScriptsPartial | 33 |

### Amir Džafić – ~2.140 linija (~39 %)
Fokus: rezervacije, popusti, korisnici i notifikacije. **CL-K + CL-S1 i CL-S3** (validacija, kontrola pristupa).

| Fajl | Linija |
|---|---|
| Controllers/RezervacijaController.cs | 270 |
| Models/Rezervacija.cs, Models/Status.cs | 50 |
| Views/Rezervacija/* (Index, Create, Edit, Details, Delete) | 528 |
| Controllers/PopustController.cs | 172 |
| Models/Popust.cs | 25 |
| Views/Popust/* | 253 |
| Controllers/KorisnikController.cs | 105 |
| Models/Korisnik.cs | 23 |
| Views/Korisnik/* (uklj. PromijeniRolu) | 385 |
| Controllers/NotifikacijaController.cs | 91 |
| Models/Notifikacija.cs | 30 |
| Views/Notifikacija/* | 208 |

### Imran Čustović – ~2.310 linija (~42 %)
Fokus: termini, tereni, izvještaji i zajednički layout. **CL-K + CL-S1 i CL-S3** (validacija, kontrola pristupa).

| Fajl | Linija |
|---|---|
| Controllers/TerminController.cs | 224 |
| Models/Termin.cs | 36 |
| Views/Termin/* | 489 |
| Controllers/TerenController.cs | 159 |
| Models/Teren.cs, Models/Sport.cs | 27 |
| Views/Teren/* | 345 |
| Controllers/IzvjestajController.cs | 121 |
| Models/Izvjestaj.cs | 44 |
| Views/Izvjestaj/* | 317 |
| Views/Shared/_Layout.cshtml, _Layout.cshtml.css | 251 |
| wwwroot/css/site.css, wwwroot/js/site.js | 296 |

**Svi inspektori dobijaju obje liste provjere (CL-K i CL-S).** Naglasak samo govori šta je za koji dio koda najvažnije.

---

## 3. Lista provjere CL-K – opća lista (Predavanje 2)

Stavke sa ★ su iz generičke liste provjere Karla E. Wiegersa (P2, slajd 38). Ostale su tipični defekti koje P2 navodi na slajdovima 12 i 18–22, u istim kategorijama.

**K1 Struktura**
- **K1.1** ★ Da li kod u potpunosti implementira dizajn / zahtjeve?
- **K1.2** ★ Koriste li se imenovane konstante umjesto „magičnih brojeva“?
- **K1.3** Ima li nedostižnog (mrtvog) ili nekorištenog koda: varijable, parametri? *(sl. 12, 18, 19, 22)*
- **K1.4** Ima li previše kompleksnih funkcija? *(sl. 12)*
- **K1.5** Jesu li interfejsi između modula i komponenti odgovarajući? *(sl. 12)*

**K2 Varijable i aritmetika**
- **K2.1** ★ Jesu li sve varijable smisleno imenovane?
- **K2.2** ★ Da li kod izbjegava direktno poređenje floating-point brojeva?
- **K2.3** Referencira li se varijabla s neodređenom (neinicijaliziranom) vrijednošću? *(sl. 12)*
- **K2.4** Ima li neodgovarajuće deklaracije varijabli ili dodjele vrijednosti istoj varijabli? *(sl. 12, 20)*

**K3 Petlje i grananja**
- **K3.1** ★ Da li svaka `switch/case` naredba ima definisan `default` slučaj?
- **K3.2** ★ Jesu li uslovi za izlazak iz petlje jasno ostvarivi (nema beskonačnih petlji)?
- **K3.3** Ima li nedostajuće ili pogrešne logike? *(sl. 12)*

**K4 Defanzivno programiranje**
- **K4.1** ★ Provjeravaju li se granice nizova i kolekcija prije pristupa indeksu?
- **K4.2** ★ Oslobađa li se zauzeta memorija i resursi nakon korištenja?

**K5 Standardi kodiranja**
- **K5.1** Krši li kod pravila i standarde kodiranja (stil, konvencije imenovanja)? *(sl. 12, 13, 16)*

---

## 4. Lista provjere CL-S – sigurnosna lista (Predavanje 3)

Stavke su iz prilagođene sigurnosne check-liste (P3, slajdovi 21–22). OWASP i CWE oznake su sa slajdova 12–16 i 23–54.

**S1 Validacija i čišćenje unosa** – OWASP A05:2025 Injection
- **S1.1** Validiraju li se na serveru svi podaci sa klijenta (forme, URL parametri, API pozivi): tip, opseg, format (`ModelState.IsValid`)?
- **S1.2** Izbjegava li se spajanje korisničkog unosa u SQL (`FromSqlRaw`, string konkatenacija)? *(CWE-89)*
- **S1.3** Kodira li se izlaz, bez `Html.Raw` nad korisničkim podacima? *(CWE-79, XSS)*

**S2 Upravljanje tajnama**
- **S2.1** Ima li u kodu ili konfiguraciji hardkodiranih lozinki, tokena, API ključeva ili connection stringova (appsettings, EmailService, Program.cs)? *(CWE-798)*
- **S2.2** Jesu li tajne izmještene u varijable okruženja ili user-secrets i van repozitorija (`.gitignore`)?

**S3 Autorizacija i kontrola pristupa** – OWASP A01:2025 Broken Access Control
- **S3.1** Je li svaka osjetljiva akcija zaštićena (`[Authorize]`, uloge), uz provjeru na serveru, a ne samo skrivanjem dugmeta u viewu? *(CWE-862, CWE-284)*
- **S3.2** Provjerava li se kod `Details/Edit/Delete(id)` da korisnik smije pristupiti baš tom zapisu? *(CWE-639, IDOR)*
- **S3.3** Imaju li POST akcije zaštitu od CSRF-a (`[ValidateAntiForgeryToken]`)? *(CWE-352)*

**S4 Kriptografija i zaštita podataka** – OWASP A04:2025 Cryptographic Failures
- **S4.1** Čuvaju li se osjetljivi podaci (lozinke) heširani ili šifrirani, a ne kao plain text? *(CWE-311)*
- **S4.2** Je li izbjegnuta upotreba MD5 / SHA-1? *(CWE-327, CWE-328)*
- **S4.3** Jesu li podaci u prenosu zaštićeni (HTTPS redirect, HSTS, SSL za SMTP)? *(CWE-319)*

**S5 Upravljanje greškama i logiranje**
- **S5.1** Prikazuje li aplikacija stack trace ili `ex.Message` korisniku (Developer Exception Page van Developmenta)? *(CWE-209)*
- **S5.2** Logiraju li se sigurnosni događaji (prijava, promjena uloge) i maskiraju li se povjerljivi podaci u logovima? *(CWE-778)*

**S6 Sigurnost vanjskih komponenti** – OWASP A03:2025 Software Supply Chain Failures
- **S6.1** Sadrži li projekat zastarjele NuGet ili JS pakete, ili Docker base image s poznatim ranjivostima? *(CWE-1395)*


---

## 5. Skala ozbiljnosti (P2, slajd 39)

| Nivo | Naziv | Značenje |
|---|---|---|
| **5** | Kritično (Block-level) | Potpuno sprječava rad osnovnih funkcija ili ugrožava sigurnost i zaštitu sistema |
| **4** | Visoko (High) | Ozbiljno utiče na ključne funkcije, troškove ili rokove; zaobilazno rješenje nije poznato |
| **3** | Srednje (Medium) | Utiče na rad funkcija, ali je privremeno zaobilazno rješenje poznato |
| **2** | Nisko (Low) | Manje neugodnosti za korisnika ili programere, ne utiču na glavnu svrhu softvera |
| **1** | Minorno (Minor) | Kozmetičke greške i sitni nedostaci (npr. tipfeleri u interfejsu) |

---

## 6. Kako se bilježe nalazi

### 6.1 Komentar na PR-u
Svaki nalaz ide kao komentar na tačnu liniju, u kartici **Files changed**. Format:

```
**Ozbiljnost:** 4 – Visoko
**Lista provjere:** CL-S3.2 – provjera pristupa zapisu (OWASP A01:2025, CWE-639)
**Opis:** Akcija Edit(id) učitava rezervaciju samo po id-u, bez provjere da li pripada prijavljenom korisniku.
```

### 6.2 Issue kartica
Prijedlog: za svaki nalaz ozbiljnosti **3, 4 i 5** otvara se i issue kartica, sa linkom na komentar ili liniju. Nalazi nivoa 1–2 mogu ostati samo kao komentari.

- **Naslov:** `[Ozb. 4][CL-S3.2] Nedostaje provjera vlasnika rezervacije u Edit`
- **Labele:** `ozbiljnost-1` … `ozbiljnost-5` i `CL-K` / `CL-S` (već kreirane u repozitoriju, vidi 7.7)
- **Assignee:** inspektor koji je našao grešku
- **Tijelo:** isti format kao komentar, plus link na liniju koda

---

## 7. Upute za inspektore – korak po korak

### 7.1 Priprema
1. Lokalno se prebaci na najnoviji kod (nije obavezno, ali je lakše čitati u IDE-u):
   ```
   git fetch
   git checkout inspekcija-tim3
   git pull
   ```
2. Na GitHubu otvori PR `inspekcija-tim3` → `inspekcija-base`, kartica **Files changed**. Tu se pišu svi komentari.
3. U tabeli iz poglavlja 2 nađi svoje fajlove. Pregledaj **samo svoje**, osim ako moderator kaže drugačije.
4. Pripremi obje liste provjere (poglavlja 3 i 4) i skalu ozbiljnosti (poglavlje 5).

### 7.2 Kako pregledati jedan fajl
1. Prvo pročitaj fajl u cjelini i shvati šta radi (npr. controller: koje akcije ima, ko ih smije pozvati, šta upisuje u bazu).
2. Zatim prođi kroz **CL-K**, stavku po stavku (K1.1 … K5.1), i za svaku se zapitaj: „Da li ovaj fajl krši ovo?“
3. Isto uradi i za **CL-S** (S1.1 … S6.1). Kod controllera posebno provjeri:
   - ima li `[Authorize]` i provjeru uloge (S3.1);
   - provjerava li `Edit/Delete(id)` da korisnik smije dirati baš taj zapis (S3.2);
   - imaju li POST akcije `[ValidateAntiForgeryToken]` (S3.3);
   - provjerava li se `ModelState.IsValid` (S1.1);
   - šta se dešava kad zapis ne postoji (K3.3).
4. Kod viewova (`.cshtml`) provjeri:
   - `Html.Raw` (S1.3);
   - skrivanje dugmadi umjesto prave zaštite na serveru (S3.1);
   - tipfelere i nejasne poruke (ozbiljnost 1, P2 sl. 39);
   - postoji li `asp-antiforgery` / `@Html.AntiForgeryToken()` u formama (S3.3).
5. Svaku nađenu grešku odmah zabilježi (7.3). Popravke **ne predlaži u kodu**, samo opiši problem.
6. Tempo: oko **150 linija na sat**. Zapisuj koliko si vremena proveo (za sumarni izvještaj).

### 7.3 Kako ostaviti komentar na GitHubu
1. U **Files changed** pređi mišem preko broja linije → klikni plavi **+**. Za više linija klikni prvu liniju, pa uz Shift zadnju.
2. Upiši komentar u formatu iz 6.1 (ozbiljnost, lista provjere, opis).
3. Klikni **Start a review** (za prvi komentar), a za sljedeće **Add review comment**. Tako se komentari skupljaju, a ne šalju jedan po jedan.
4. Kad završiš sve svoje fajlove: gore desno **Finish your review** → odaberi **Comment** (ne „Approve“ i ne „Request changes“) → **Submit review**.

### 7.4 Kako otvoriti issue karticu (za ozbiljnost 3–5)
1. Na komentaru u PR-u klikni **…** → **Reference in new issue**. Ili otvori **Issues → New issue** i zalijepi link na liniju.
2. Naslov: `[Ozb. X][CL-YY] kratak opis` (vidi 6.2).
3. Desno dodaj **Labels** (`ozbiljnost-X` i `CL-K` ili `CL-S`) i **Assignees** (sebe).
4. **Submit new issue**.

### 7.5 Šta NE raditi
- Ne mijenjati kod i ne pushati na `inspekcija-tim3` ni `inspekcija-base`.
- Ne mergati i ne zatvarati PR.
- Ne kopirati lozinke ni ključeve iz koda u komentare. Dovoljno je navesti fajl i liniju.
- Ne pisati komentare o autoru, samo o kodu.

### 7.6 Kad završiš
Javi moderatoru:
- da si završio;
- koliko si nalaza ostavio;
- koliko si sati proveo.

Na sastanku inspekcije zajedno prolazimo kroz nalaze i usaglašavamo ozbiljnost.

### 7.7 Labele u repozitoriju

✅ Moderator je već kreirao sve labele u repozitoriju. Inspektori ih samo biraju pri otvaranju issue kartice i ne prave nove.

| Label | Opis | Boja |
|---|---|---|
| `ozbiljnost-5` | Kritično – sprječava rad osnovnih funkcija ili ugrožava sigurnost | `#B60205` |
| `ozbiljnost-4` | Visoko – ozbiljno utiče na ključne funkcije, nema zaobilaznog rješenja | `#D93F0B` |
| `ozbiljnost-3` | Srednje – utiče na funkcije, postoji zaobilazno rješenje | `#FBCA04` |
| `ozbiljnost-2` | Nisko – manje neugodnosti, ne utiče na glavnu svrhu | `#0E8A16` |
| `ozbiljnost-1` | Minorno – kozmetičke greške, tipfeleri | `#C5DEF5` |
| `CL-K` | Lista provjere – kvalitet koda (Wiegers, Predavanje 2) | `#5319E7` |
| `CL-S` | Lista provjere – sigurnost (OWASP/CWE, Predavanje 3) | `#1D76DB` |

---

## 8. Pravila (P2, slajdovi 35–37 i 43)

1. **Samo detekcija, bez popravljanja.** Inspektori ne mijenjaju kod i ne pushaju na `inspekcija-tim3`.
2. **PR se ne merga.** Ostaje otvoren dok inspekcija traje i služi samo za komentare.
3. **Tempo:** oko 150 linija na sat, sporije i fokusirano.
4. **Komentari su o kodu, ne o autoru.**
5. **Sastanak inspekcije** traje najviše 2 sata. Moderator vodi sastanak i bilježi lokaciju, tip i ozbiljnost svakog nalaza.
6. **Svaki inspektor vodi evidenciju utrošenog vremena** (priprema i sastanak), za sumarni izvještaj: gustoća defekata, sati po defektu, brzina pregleda.

---

## 9. Okvirni raspored

| Faza | Šta | Ko | Rok |
|---|---|---|---|
| 1. Planiranje | Podjela koda i lista provjere (ovaj dokument) | Dino | ___ |
| 2. Priprema | Samostalni pregled dodijeljenog koda, komentari i issue kartice na PR-u | svi | ___ |
| 3. Sastanak | Zajednički prolaz kroz nalaze, usaglašavanje ozbiljnosti | svi, vodi Dino | ___ |
| 4. Izvještaj | Izvještaj o inspekciji + Pareto analiza | Dino (+ svi) | ___ |
| 1.2 Statička analiza | Linter (Roslyn analizatori / SonarLint) nad istim kodom + komentar u izvještaju | ___ | ___ |

**Rok za Task 1:** 4. laboratorijska vježba.
