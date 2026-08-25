# DPSHTRR — Dokumentacion i testeve automatike

**Data:** 25.08.2026  
**Folder:** `MS Automation refactor/DPSHTRR`  
**Framework:** NUnit + Selenium WebDriver (Microsoft Edge)  
**Mjedisi:** `http://141.95.84.12:8080/`  
**Microservice:** `dpshtrr-merge-not-ams_refactor`

Ky dokument përshkruan të gjitha testet automatike të shërbimeve DPSHTRR (aplikime + interogime) që ekipi mund t’i ekzekutojë dhe t’i përdorë si referencë.

---

## 1. Përmbledhje

| Kategoria | Numri i testeve |
|---|---|
| Happy path (aplikim / sukses ose listë) | 13 |
| FailCase — stimulim dërgimi pa dokumente / validim | 8 |
| FailCase — popup Gjendja Civile | 8 |
| FailCase — popup QKB (Organisation) | 8 |
| **Total** | **37** |

---

## 2. Të dhëna të përbashkëta

| Fusha | Vlera |
|---|---|
| NID (happy path + FailCase dërgimi) | `J25730113W` |
| NID (popup Gjendja Civile) | `J55728107H` |
| NID / NIPT (popup QKB) | `M55555555E` |
| ProfileType happy path / FailCase dërgimi / Gjendja Civile | `Individual` |
| ProfileType popup QKB | `Organisation` |
| Platform | `WEB` |
| UserName | `Ketjona` |
| Email | `ketjona.mema@kreatx.com` |
| Phone | `0676041404` |

Artefaktet e dështimit ruhen te `bin/Debug/net8.0/TestArtifacts/<EmriTestit>_<timestamp>/` (screenshot + PageSource).

---

## 3. Shërbimet e mbuluara

### 3.1 Aplikime (wizard me hapa)

| Kodi | Shërbimi | Skedar happy path | Skedar FailCase |
|---|---|---|---|
| 10092 | Regjistrim ciklomotori | `10092.cs` | `10092_FailCase.cs` |
| 10094 | Regjistrim mjeti | `10094.cs` | `10094_FailCase.cs` |
| 10096 | Aplikim për ndërrim targe | `10096.cs` | `10096_FailCase.cs` |
| 10098 | Aplikim për ndërrim leje qarkullimi | `10098.cs` | `10098_FailCase.cs` |
| 10100 | Ndërrim pronësie | `10100.cs` | `10100_FailCase.cs` |
| 10109 | Ndryshim gjeneralitetesh | `10109.cs` | `10109_FailCase.cs` |
| 10112 | Rimarrje leje drejtimi | `10112.cs` | `10112_FailCase.cs` |
| 14111 | Konvertim leje drejtimi | `14111.cs` | `14111_FailCase.cs` |

### 3.2 Interogime / lista (pa FailCase)

| Kodi | Shërbimi | Testi | Skedar |
|---|---|---|---|
| 39 | Automjetet e mia | `AutomjeteteMia` | `39-NID.cs` |
| 5016 | Gjendja aktive e mjetit | `GjendjaAktiveeMjetit` | `5016.cs` |
| 5017 | Gjobat e kontrollit teknik | `GjobatKontrollitTeknik` | `5017.cs` |
| 772 | Taksa vjetore | `TaksaVjetore` | `772.cs` |
| 879 | Kundërvajtje rrugore | `KundravajtjeRrugore` | `879.cs` |

---

## 4. Happy path — çfarë verifikojnë

### 4.1 Aplikimet (10092, 10094, 10096, 10098, 10100, 10109, 10112, 14111)

Rrjedha e përgjithshme:

1. Hap service tester-in, ngarkon shërbimin me NID `J25730113W`.
2. Klikon **Aplikimi i Ri**.
3. Kalon hapat (Drejtoria Rajonale kur ekziston → të dhënat e aplikantit → informacion specifik → dokumentacioni).
4. Verifikon të dhënat e aplikantit (emër, mbiemër, atësi, datëlindje, kontakt).
5. Ngarkon dokumentet e detyrueshme (kur shërbimi i ka) dhe klikon checkbox-in e pëlqimit.
6. Klikon **Dërgo**.
7. **Kriteri i suksesit:** shfaqet `APLIKIMI JUAJ U DËRGUA ME SUKSES.`

**Shënim 10112 / 14111:** hapi i parë është drejtpërdrejt të dhënat e aplikantit (nuk ka hap Drejtorie Rajonale si te 10092–10109).

### 4.2 Interogimet

| Testi | Qëllimi |
|---|---|
| `AutomjeteteMia` | Hap listën e mjeteve, filtrin «Kërko», pret rreshtin bosh me mesazhin `Nuk dispononi mjete!` |
| `GjendjaAktiveeMjetit` | Hap shërbimin 5016 dhe verifikon titullin / formën e kërkimit |
| `GjobatKontrollitTeknik` | Hap shërbimin 5017 dhe verifikon titullin |
| `TaksaVjetore` | Hap shërbimin 772 dhe verifikon titullin |
| `KundravajtjeRrugore` | Hap shërbimin 879 dhe verifikon titullin |

---

## 5. FailCase — tre lloje për çdo aplikim

Çdo shërbim aplikimi ka **3 teste FailCase** në të njëjtin skedar `*_FailCase.cs`.

### 5.1 Stimulim dërgimi (`*_FailCase_ReturnsUiMessage`)

**Qëllimi:** aplikimi **nuk** duhet të dërgohet me sukses. Pritet një mesazh UI (informativ DPSHTRR ose gabim/validim).

**Si stimulohet:**

| Shërbimi | Stimulimi |
|---|---|
| 10092, 10094, 10096, 10098, 10100, 10109 | Plotësohen hapat; **nuk ngarkohen dokumente**; klikohet checkbox nëse ekziston; klikohet **Dërgo** |
| 10112 | Në hapin «KËRKESËS» **nuk plotësohen** fushat e detyrueshme; klikohet Vazhdo/Dërgo |
| 14111 | Në hapin e dokumentacionit klikohet **Dërgo** pa ngarkuar dokumentet e detyrueshme |

**Rezultati i pritur:**

| Në UI | Statusi i testit |
|---|---|
| `APLIKIMI JUAJ U DËRGUA ME SUKSES` | **Failed** (stimulimi dështoi) |
| Modal «Kujdes» për **aplikime ekzistuese** | **Failed** |
| Mesazh informativ DPSHTRR (p.sh. `Nuk u gjet asnje mjet` → `NoVehiclesFound`) | **Passed** |
| Çdo mesazh tjetër gabimi/validimi | **Passed** |

Mesazhet informative njihen nga `DpshtrrFailCaseSupport` sipas çelësave të backend-it, ndër të tjera:

`NoVehiclesFound` / `NoVehicalesFound`, `PersonNotFound`, `SubjectNotFound`, `CarInfoNotFound`, `CarInfoServerError`, `NoFinesFound`, `DPSHTRRUnavailable`, `TimeoutError`, `ServiceConnectionError`, `InvalidYear`, `NoServiceFound`, `VehicleExistsInSystem`, `FailedPayment`, etj.

### 5.2 Popup Gjendja Civile (`*_FailCase_GjendjaCivile_ReturnsGabimPopup`)

- NID: **`J55728107H`**
- ProfileType: **Individual**
- Në hapin e të dhënave të aplikantit pret popup:

| Fusha | Vlera e pritur |
|---|---|
| Titulli | `Gabim` |
| Përshkrimi | `Nuk u arrit të merren të dhënat nga Gjendja Civile. Ju lutemi provoni përsëri më vonë.` |

- Popup tjetër **ose** asnjë popup → **Failed**, me mesazhin që u shfaq në UI.

Te **10112** dhe **14111** popup-i pretet menjëherë pas **Aplikimi i Ri** (të dhënat e aplikantit janë hapi i parë).

### 5.3 Popup QKB (`*_FailCase_Qkb_ReturnsGabimPopup`)

- NID: **`M55555555E`**
- ProfileType: **Organisation**
- Në hapin e të dhënave të aplikantit pret popup:

| Fusha | Vlera e pritur |
|---|---|
| Titulli | `Gabim` |
| Përshkrimi | `Nuk u arrit të merren të dhënat nga QKB. Ju lutemi provoni përsëri më vonë.` |

- Popup tjetër **ose** asnjë popup → **Failed**, me mesazhin që u shfaq në UI.

---

## 6. Inventari i plotë i metodave

### Happy path

| Test | Skedar |
|---|---|
| `Regjistrim_Ciklomotori` | `10092.cs` |
| `Regjistrim_Mjeti` | `10094.cs` |
| `Aplikim_Per_Nderrim_Targe` | `10096.cs` |
| `Aplikim_Per_Nderrim_LejeQarkullimi` | `10098.cs` |
| `Ndrimm_Pronesie` | `10100.cs` |
| `Ndryshim_Gjeneralitetesh` | `10109.cs` |
| `Rrimarrje_Leje_Drejtimi` | `10112.cs` |
| `KonvertimLejesDrejtimit` | `14111.cs` |
| `AutomjeteteMia` | `39-NID.cs` |
| `GjendjaAktiveeMjetit` | `5016.cs` |
| `GjobatKontrollitTeknik` | `5017.cs` |
| `TaksaVjetore` | `772.cs` |
| `KundravajtjeRrugore` | `879.cs` |

### FailCase (24 teste)

Për çdo shërbim aplikimi: `*_ReturnsUiMessage`, `*_GjendjaCivile_ReturnsGabimPopup`, `*_Qkb_ReturnsGabimPopup`.

| Shërbimi | Prefiksi i metodës |
|---|---|
| 10092 | `Regjistrim_Ciklomotori_FailCase_*` |
| 10094 | `Regjistrim_Mjeti_FailCase_*` |
| 10096 | `Aplikim_Per_Nderrim_Targe_FailCase_*` |
| 10098 | `Aplikim_Per_Nderrim_LejeQarkullimi_FailCase_*` |
| 10100 | `Ndrimm_Pronesie_FailCase_*` |
| 10109 | `Ndryshim_Gjeneralitetesh_FailCase_*` |
| 10112 | `Rrimarrje_Leje_Drejtimi_FailCase_*` |
| 14111 | `KonvertimLejesDrejtimit_FailCase_*` |

---

## 7. Si të ekzekutohen

Nga Test Explorer (Visual Studio / Rider) ose nga terminali, p.sh.:

```bash
dotnet test "MS Automation refactor.csproj" --filter FullyQualifiedName~DPSHTRR
```

Ose një shërbim i vetëm:

```bash
dotnet test "MS Automation refactor.csproj" --filter FullyQualifiedName~_10092_FailCase_
```

Browser: Microsoft Edge, i maksimizuar. Testet hapin session të ri për çdo `[SetUp]`.

---

## 8. Shënime për ekipin

1. **Happy path** kërkon NID me të dhëna të vlefshme në Gjendje Civile (`J25730113W`) dhe, për shërbimet e mjeteve, kushte që lejojnë dërgimin (dokumente, mjet kur kërkohet).
2. **FailCase dërgimi** nuk duhet të kalojë nëse del ekrani i suksesit ose «Kujdes» për aplikim ekzistues. Mesazhe si «Nuk u gjet asnje mjet» janë të pranueshme.
3. **Gjendja Civile / QKB** janë raste negative të kontrolluara me NID të fiksuar; titulli dhe përshkrimi duhet të përputhen fjalë për fjalë.
4. Logjika e përbashkët e FailCase-eve është te `DpshtrrFailCaseSupport.cs`.
5. Interogimet (39, 5016, 5017, 772, 879) nuk kanë FailCase në këtë paketë.

---

## 9. Matrica e shpejtë (për review)

| Kodi | Happy path | Fail dërgimi | Fail Gjendja Civile | Fail QKB |
|---|---|---|---|---|
| 10092 | Po | Po | Po | Po |
| 10094 | Po | Po | Po | Po |
| 10096 | Po | Po | Po | Po |
| 10098 | Po | Po | Po | Po |
| 10100 | Po | Po | Po | Po |
| 10109 | Po | Po | Po | Po |
| 10112 | Po | Po | Po | Po |
| 14111 | Po | Po | Po | Po |
| 39 | Po | — | — | — |
| 5016 | Po | — | — | — |
| 5017 | Po | — | — | — |
| 772 | Po | — | — | — |
| 879 | Po | — | — | — |
