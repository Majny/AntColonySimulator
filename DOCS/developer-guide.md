# Ant Colony – Programátorská dokumentace (kompletní)

Tento dokument popisuje architekturu, toky dat, klíčové třídy/komponenty, detailní pohyb a steering mravenců, feromonový systém, genom, editor a runtime UI, doporučení k výkonu, ladění a rozšíření.

Legenda zkratek:
- TM = TeamManager
- GR = GameRules
- PF = PheromoneField

--------------------------------------------------------------------------------

## Obsah

- 1) Životní cyklus a toky dat
- 2) Architektura tříd
- 3) AntAgent – pohyb a steering
  - 3.1 Skládání vektorů
  - 3.2 Update
  - 3.3 Random steering
  - 3.4 Pheromone steering (L/C/R)
  - 3.5 Target steering (food/nest)
  - 3.6 Překážky: antény, klouzání, otočky
  - 3.7 Hranice herní oblasti
- 4) PheromoneField – úložiště a vizualizace
- 5) Genom a **fluent syntax**
  - 5.1 Jak multiplikátory působí
  - 5.2 Fluent randomizer
  - 5.3 Příklady použití
- 6) GameRules – globální přepínače a intervaly
- 7) TeamManager – správa týmů a sdílených polí
- 8) NestController – hnízdo a spawn
- 9) FoodSpawner a FoodItem
- 10) Editor a UI
- 11) PlayAreaRectBoundary
- 14) PlantUML diagramy (text k uložení do souborů)

--------------------------------------------------------------------------------

## 1) Životní cyklus a toky dat

Start scény:
- GameRules.Awake a TeamManager.Awake nastaví základ editoru.
- TeamManager vytvoří slovník týmů a případně fieldsRoot (parent pro PF).

Editor -> simulace:
- LevelEditor umisťuje Nest a FoodSpawner, kreslí Dirt. NestController je vypnutý.
- Kliknutí Start v UI zapne všechny NestController:
  - NestController.Start zavolá TeamManager.GetOrCreateTeamFields(teamId, ...) a zaregistruje hnízdo.
  - Proběhne SpawnAgent pro initialAgents.

Běh simulace:
- AntAgent v Update skládá vektory řízení a integruje pohyb, řeší překážky a feromony.
- PheromoneField ukládá kapky a poskytuje sampling pro senzory L/C/R.
- DepositFood v hnízdě zvyšuje skóre v TM a může spustit Simulation of Life.

--------------------------------------------------------------------------------

## 2) Architektura tříd (přehled)

- TeamManager: autorita nad týmy, skóre, PF. API: RegisterNest/Ant, UnregisterAnt, AddFood, GetOrCreateTeamFields, GetTeam* metody, GetAll.
- NestController: spouští AntAgent, aplikuje genom, přijímá reporty jídla a auto-spawnuje nové agenty při Simulation of Life.
- AntAgent: stavový automat ToFood/ToHome, steering (random/pheromone/target/obstacles/turn), integrace pohybu, pokládání/čtení feromonů.
- GameRules: přepínače (simulationOfLife, upgradedAnts), GenerateGenome.
- AntGenome: multiplikátory násobící AgentParameters, Clamp, Multiply, ApplyTo.
- PheromoneField: grid buněk, sampling v kruhu, vizualizace přes ParticleSystem.
- FoodSpawner: Build blobs, spawn food, rebuild
- Editor a UI: LevelEditor + LevelEditorUI, scoreboard, PF visibility cycler, drag panel, time scale.

--------------------------------------------------------------------------------

## 3) AntAgent – pohyb a steering (detailní, rozšířený)

Tato kapitola rozebírá, **jak přesně vzniká výsledný vektor řízení** (`steer`) a jak se jednotlivé složky ovlivňují. Text odpovídá kódu ve třídě `AntAgent` (hlavně `Update()`, `IntegrateMovement()` a `Handle*` metody).

---

### 3.1 Skládání vektorů (hlavní princip)

**Hlavní vektor řízení** je v každém snímku **součtem** krátkodobých cílů a až **poté** je normalizován. Tím se přirozeně projeví relativní síla jednotlivých složek, ale výsledná velikost tahu je udržena v normě.

**Z čeho se skládá:**
- `randomSteer` – jemná explorace (preferuje směr blízký aktuálnímu `heading`).
- `pheromoneSteer` – skenování podle feromonové mapy (trojice senzorů L/C/R).
- `targetSteer` – přitahuje k bezprostřednímu cíli (jídlo / hnízdo).
- `obstacleAvoidForce` – okamžitý úkrok od překážek.
- `turnSteerForce` – dočasný impulz pro řízenou otočku (po nárazu, po odevzdání jídla).



Poznámky:
- **Normalizace po součtu** zabraňuje přefouknutí výsledné síly.
- **`acceleration`** řídí, jak rychle se aktuální rychlost přibližuje k cílové (plynulé zatáčení).
- **Implicitní priority** vznikají přirozeně: např. `obstacleAvoidForce` je „tvrdý“ úkrok, kdežto `randomSteer` je „měkké“ vodítko.

---

### 3.2 Update

1. `MaybeRefreshLegTimers()` – udržuje stáří pachu, aby kapky správně slábly a obnovovaly se.
2. `PlacePheromoneIfNeeded()` – pokud se urazila vzdálenost `pheromoneSpacing` a pach nevyprchal, položí novou kapku do příslušného PF.
3. `HandleRandomSteering()` – periodicky přehazuje náhodný směr podobný `heading` (a vypíná se, když je zaměřeno jídlo).
4. `HandlePheromoneSteering()` – vyhodnotí senzory L/C/R, zvolí nejsilnější směr a nastaví `pheromoneSteer` (s throttlingem).
5. Stav:
   - **ToFood →** `HandleFoodSeeking()` (nalezení cíle, pickup).
   - **ToHome →** `HandleReturnHome()` (odevzdání v hnízdě, jinak návrat hlavně po stopě).
6. `HandleCollisionSteering()` – antény proti kolizím.
7. `IntegrateMovement()` – součet složek, kolize (vytlačení + `SlideAlong`), případně `StartTurnAround`, posun a natočení.
8. `LateUpdate()` → `EnforcePlayArea()` – přicvaknutí k hranici arény PF a otočka směrem do středu.
„antény“ dopředu, úkrok do volné strany, krátký cooldown a jemný šum do stejné strany.
---

### 3.3 `randomSteer` – jak vzniká a kdy se potlačuje

- **Vznik:** v intervalu `randomSteerMaxDuration` se vygeneruje několik náhodných unit vektorů, vybere se nejpodobnější směr dopředu vůči `heading`. Výsledek se škáluje pomocí `parameters.randomSteerStrength`.
- **Dopad:** dává trajektorii život a pomáhá objevovat.

---

### 3.4 `pheromoneSteer` – L/C/R senzory

- **Senzory:** z `heading` odvodí směry `-angle`, `0`, `+angle` (kde `angle = pheromoneSensorAngle`). Pro každý směr se vzorkuje bod ve vzdálenosti `pheromoneSensorDistance` a integruje se síla v okruhu `pheromoneSensorSize`.
- **Blokace zdí:** `Physics2D.Linecast(from, pos, obstacleMask)` → pokud narazí, senzor vrací **0** (senzor „nevidí“ za překážku).
- **Výběr pole:** v módu **ToFood** se čte **foodField**, v módu **ToHome** se čte **homeField**.
- **Volba směru:** vybere se největší z L/C/R; `pheromoneSteer = unit(dir) * steerStrength`.
- **Interakce:** v **ToFood** při aktivním cíli se `pheromoneSteer` oslabí, aby měl přednost `targetSteer`.

---

### 3.5 `targetSteer` – přitahování k jídlu / bonus na hnízdo

**ToFood:**
- `AcquireTargetFood()` vybírá nejbližší vhodný collider s jídlem v `detectionRadius`.
- Pokud **nejbližší bod collideru** je v dosahu `pickupDistance + GetTargetRadius(col)`, volá se `PickupFoodTarget()`.

**ToHome:**
- Uvnitř hnízda (`OverlapPoint`), `DepositFood()` a přepnutí do **ToFood**.
- Jinak standardně `targetSteer = 0` (návrat po stopě). Bonus na hnízdo se přidá, jen pokud je v dosahu a je k němu čistý LOS.

---

### 3.6 `obstacleAvoidForce`, `SlideAlong`, `turnSteerForce`

#### Antény (raycasty):
- Z levé/pravé antény (offset `antennaOffset` kolmo na `heading`) letí ray o délce `antennaDistance`.
- Pokud dojde k zásahu, vybere se **bližší** strana a nastaví se `obstacleAvoidForce = ±side * collisionAvoidSteerStrength`.
- Nastaví se krátký **cooldown** (`obstacleResetTime`) a do `randomSteer` se přidá jemný šum **do stejné strany** (aby se trajektorie „utrhla“).

#### Kolize a `SlideAlong`:
- **Uvnitř collideru:** `OverlapCircle` → agent je vytlačen na okraj (po normále) a zavolá se `SlideAlong(n)`.
- **Předvídání nárazu:** `CircleCast` → posun na kontakt a `SlideAlong(hit.normal)`.
- **`SlideAlong` dělá:**
  - najde tečnu k normále, která je nejbližší preferovanému směru,
  - nastaví rychlost po tečně na rozumnou velikost (poměr `maxSpeed`/aktuální),
  - přidá malý šum a krátký cooldown vyhýbání.

#### Řízená otočka:
- `StartTurnAround()` nastaví `isTurning`, `turnEndTime`.

---

### 3.7 Hranice herní oblasti

- `EnforcePlayArea()` čte obdélník z `PheromoneField.GetWorldRect()`.
- Pokud je agent mimo, přicvakne jeho pozici na hranici, otočí `heading` ke středu a spustí řízenou otočku.
- Zabraňuje úniku mimo arénu.


--------------------------------------------------------------------------------

## 4) PheromoneField – úložiště a vizualizace

Úložiště:
- Area je světový obdélník PF, buňka vychází z agentParams.pheromoneSensorSize.
- Buňka = LinkedList<Entry> { position, initialWeight, creationTime }.
- Evaporace: age > EvapTime -> záznam se odstraní; jinak váha lineárně klesá.

Add:
- Vloží Entry do buňky a emituje částici s alfou podle initialWeight.

SampleStrength:
- Sečte váhy v okruhu radius kolem dotazu, při průchodu zároveň odpaří expirované záznamy.

Vizualizace:
- ParticleSystem v World space
- SetVisible(bool) pro přepínání zobrazení (cykler na F).

--------------------------------------------------------------------------------

## 5) Genom a fluent syntax

Genom (AntGenome) je sada multiplikátorů, které se násobí do AgentParameters a tím mění chování agenta.

### 5.1 Jak multiplikátory působí

- speedMult -> maxSpeed  
- accelMult -> acceleration  
- steerMult -> steerStrength  
- sensorDistanceMult -> pheromoneSensorDistance  
- randomSteerMult -> randomSteerStrength  
- pheromoneRunOutMult -> pheromoneRunOutTime  
- pheromoneSpacingMult -> pheromoneSpacing

Doplňky:
- Clamp(min, max) – pojistka proti extrémům.
- Multiply(other) – vektorové násobení genomů, momentálně  nepoužíváme, ale dobré pro rozšíření.
- ApplyTo(AgentParameters) – násobí hodnoty v klonu parametrů.

### 5.2 Fluent randomizer (Rand)

- AntGenomeRandomizer umožňuje randomizovat jednotlivé multiplikátory fluent stylem.
- FromRules(GameRules) přečte intervaly z GR a randomizuje všechny parametry.
- Done() vrátí hotový genom.

### 5.3 Příklady použití (včetně jiného než defaultního)

A) Vypnuto (žádné upgrady)
    
    var chosen = ScriptableObject.Instantiate(agentParams).Apply(null);

B) Jednoduchá náhoda z GameRules
    
    var g = AntGenome.Random(GameRules.Instance);
    var chosen = ScriptableObject.Instantiate(agentParams).Apply(g);

C) Fluent + Clamp + buff stacking
    
    var rules = GameRules.Instance;
    
    var baseG = AntGenome.Create()
        .Rand().FromRules(rules).Done()
        .Clamp(0.7f, 1.5f); // pojistka
    
    var teamBuff = AntGenome.Create().WithSpeed(1.10f).WithSteer(1.05f);
    var final = baseG.Multiply(teamBuff);
    
    var chosen = ScriptableObject.Instantiate(agentParams).Apply(final);

D) Ruční profil bez náhody (jiné použití než default)
    
    // ultra průzkumník: dlouhé senzory, slabší inkoust, delší rozestupy
    var scout = AntGenome.Create()
        .WithSensorDistance(1.35f)
        .WithRandomSteer(1.15f)
        .WithPheroRunOut(0.75f)
        .WithPheroSpacing(1.25f)
        .Clamp(0.6f, 1.8f);
    
    var chosen = ScriptableObject.Instantiate(agentParams).Apply(scout);

E) Deterministická náhoda se seedem
    
    var rng = new System.Random(42);
    float Lerp(Vector2 r) => Mathf.Lerp(r.x, r.y, (float)rng.NextDouble());
    
    var rules = GameRules.Instance;
    var g = AntGenome.Create()
        .WithSpeed(Lerp(rules.speedMult))
        .WithAccel(Lerp(rules.accelMult))
        .WithSteer(Lerp(rules.steerMult))
        .WithSensorDistance(Lerp(rules.sensorDistanceMult))
        .WithRandomSteer(Lerp(rules.randomSteerMult))
        .WithPheroRunOut(Lerp(rules.pheromoneRunOutMult))
        .WithPheroSpacing(Lerp(rules.pheromoneSpacingMult))
        .Clamp(0.7f, 1.5f);
    
    var chosen = ScriptableObject.Instantiate(agentParams).Apply(g);

F) Event buff za běhu (Multiply)
    
    // dočasný power-up: zvýšení rychlosti a steeru o 20 %
    var powerUp = AntGenome.Create().WithSpeed(1.2f).WithSteer(1.2f);
    currentGenome = currentGenome.Multiply(powerUp);
    // případně přepočítat AgentParameters, pokud chceme okamžitý efekt

--------------------------------------------------------------------------------

## 6) GameRules – globální přepínače a intervaly

- simulationOfLife: každých N kusů jídla přidá nový spawn (NestController.ReportFood).
- upgradedAnts: při spawnu se použije genom (Random z intervalů, nebo vlastní fluent varianta).


--------------------------------------------------------------------------------

## 7) TeamManager – správa týmů a sdílených polí

- V Awake předvytvoří sloty pro 0..MaxTeams-1 (barva, jméno).
- GetOrCreateTeamFields: zajistí homeField a foodField pro tým (z prefabu nebo z default prefabu).
- RegisterNest/Ant, UnregisterAnt, AddFood – statistiky a scoreboard.
- GetTeamFoodCount/GetTeamAntCount/GetAll – dotazovací API pro UI a utility.

--------------------------------------------------------------------------------

## 8) NestController – hnízdo a spawn

- Start: vyžádá týmová PF u TeamManageru, registruje se a spawne počáteční agenty.
- SpawnAgent:
  - vytvoří instanci AntAgent, vybere parametry: SO přímo nebo klon + Apply(genome),
  - Init(this, teamHomeField, teamFoodField), obarví rendery barvou týmu,
  - registrace v TeamManageru.
- ReportFood:
  - inkrement lokálního počítadla, scoreboard přes TM.AddFood,
  - Simulation of Life: akumuluje jídlo a při dosažení foodPerNewAnt spustí SpawnAgent.

--------------------------------------------------------------------------------

## 9) FoodSpawner a FoodItem

- FoodSpawner drží počet blobů a podle maintainAmount je dá do vyznačeného prostotu.
- FoodItem má TryTake a flag taken pro bezpečné sebrání více agenty.

--------------------------------------------------------------------------------

## 10) Editor a UI

- LevelEditor: nástroje Food/Nest/Dirt/Rubber, team index, poloměr štětce, Start/Reset. Umí kreslit Dirt jako kruhové segmenty s kolizí v dané vrstvě.
- LevelEditorUI: drátuje UI prvky na LevelEditor a GameRules, synchronizuje slider poloměru se stavem editoru.
- TeamScoreboard: pravidelně rebuilduje řádky podle TeamManageru.
- PheromoneVisibilityCycler: klávesou F přepíná, který tým má viditelné PF (nebo žádný).

--------------------------------------------------------------------------------

## 11) PlayAreaRectBoundary

- Z PF.GetWorldRect sestaví 4 box collidery jako zdi. Volitelně vykreslí outline pomocí LineRendereru.
- Při změně hodnot v editoru udělá rebuild, aby odpovídal aktuálním hodnotám.

--------------------------------------------------------------------------------
