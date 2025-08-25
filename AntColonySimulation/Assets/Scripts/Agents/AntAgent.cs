using UnityEngine;

public class AntAgent : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────────
    // KONFIGURACE Z INSPECTORU (externí zdroje a vrstvy)
    // ─────────────────────────────────────────────────────────────────────────────

    [Header("Config & refs")]
    public AgentParameters parameters;     // ScriptableObject s parametry chování
    public Transform sensorOrigin;         // Bod, ze kterého agent snímá raycasty
    public Transform head;                 // Místo, kam se přichytí nasbírané jídlo
    public LayerMask foodLayer;            // Vrstva pro vyhledávání jídla
    public LayerMask nestLayer;            // Vrstva pro detekci hnízda
    public LayerMask obstacleMask;         // Vrstva překážek

    // ─────────────────────────────────────────────────────────────────────────────
    // RUNTIME ODKAZY – doplní NestController při spawnování
    // ─────────────────────────────────────────────────────────────────────────────
    PheromoneField homeField;  // Feromonové pole ToHome
    PheromoneField foodField;  // Feromonové pole To Food

    private NestController nestRef; // Reference na vlastní hnízdo (svého týmu)
    PheromoneField playArea;        // Hranice herní plochy


    // ─────────────────────────────────────────────────────────────────────────────
    // STAV AGENTA A CÍLE
    // ─────────────────────────────────────────────────────────────────────────────
    AntMode mode;                // Režim: ToFood (hledám jídlo) / ToHome (nesu domů)
    Transform carriedItem;       // Co právě nese (root předmětu/food)
    Transform targetFood;        // Aktuální cíl – jídlo
    Collider2D targetFoodCol;    // Collider jídla (kvůli přesnému dosažení přes ClosestPoint)

    // ─────────────────────────────────────────────────────────────────────────────
    // VEKTORY ŘÍZENÍ POHYBU
    // ─────────────────────────────────────────────────────────────────────────────
    Vector2 heading, velocity;            // Směr a aktuální rychlost
    Vector2 randomSteer, pheromoneSteer;  // Náhodné řízení + řízení podle feromonů
    Vector2 targetSteer;                  // Směřování k cíli (jídlo / hnízdo)
    Vector2 obstacleAvoidForce;           // Vyhýbání překážkám (tykadla)

    // ─────────────────────────────────────────────────────────────────────────────
    // ČASOVAČE / PERIODICKÉ AKTUALIZACE
    // ─────────────────────────────────────────────────────────────────────────────
    float nextRandomSteerTime;      // Kdy znovu přehodit náhodné řízení
    Vector2 lastPheromonePos;       // Kde byla položená poslední kapka feromonu
    float timeSinceLeftNest,        // Čas posledního "odchodu" z hnízda (ovlivňuje sílu stopy domů)
          timeSinceLeftFood;        // Čas posledního "odchodu" od jídla (ovlivňuje sílu stopy k jídlu)

    float nextLegRefreshTime;                 // Throttling aktualizace leg timerů (viz MaybeRefreshLegTimers)
    const float LegRefreshInterval = 0.15f;   // Interval, jak často refreshnout (šetří fyziku)

    // ─────────────────────────────────────────────────────────────────────────────
    // OTOČKY / DOČASNÉ MANÉVRY
    // ─────────────────────────────────────────────────────────────────────────────
    bool isTurning;                // Probíhá řízená otočka?
    Vector2 turnSteerForce;        // Síla/vektor otočky
    float turnEndTime;             // Kdy otočku ukončit

    // ─────────────────────────────────────────────────────────────────────────────
    // SENZORY A ANTÉNY
    // ─────────────────────────────────────────────────────────────────────────────
    float nextSensorSampleTime;    // Throttling čtení feromonů
    enum Antenna { None, Left, Right }
    Antenna lastAntennaHit = Antenna.None;   // Která "tykadla" naposledy narazila
    float obstacleResetTime;                  // Dokdy držet vyhýbací sílu po zjištěné překážce

    // ─────────────────────────────────────────────────────────────────────────────
    // POMOCNÉ ALLOKAČNÍ BUFRY (bez GC)
    // ─────────────────────────────────────────────────────────────────────────────
    readonly Collider2D[] foodBuffer = new Collider2D[8]; // Pro OverlapCircleNonAlloc při hledání jídla

    // ─────────────────────────────────────────────────────────────────────────────
    // TÝMOVÉ INFO
    // ─────────────────────────────────────────────────────────────────────────────
    int myTeamId = -1;            // Pro statistiky v TeamManageru

   
    // ─────────────────────────────────────────────────────────────────────────────
    // Inicializace – volá NestController po vytvoření agenta
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Inicializace
    public void Init(NestController nest, PheromoneField home, PheromoneField food)
    {
        nestRef   = nest;
        homeField = home;
        foodField = food;

        myTeamId = nestRef.TeamId;

        if (!sensorOrigin) sensorOrigin = transform;

        // Náhodná orientace při startu
        transform.rotation = Quaternion.Euler(0, 0, Random.value * 360f);
        heading = transform.right;
        velocity = heading * parameters.maxSpeed;
        mode = AntMode.ToFood; // Začínáme hledáním jídla

        lastPheromonePos = transform.position; // "výchozí" bod pro vzdálenostní prah feromonu (aby nám to nehodil hned do hnízda)
        timeSinceLeftNest = Time.time;          // nově opustil hnízdo

        // Rozfázování náhodného řízení / senzorů, ať nejsou všichni sync
        ScheduleNextRandomSteer();
        nextSensorSampleTime = Random.value * parameters.timeBetweenSensorUpdate;

    }
    #endregion

    // ─────────────────────────────────────────────────────────────────────────────
    // Hlavní smyčka – skládání řízení, stavové chování, vyhýbání, integrace pohybu
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Unity Loop
    void Update()
    {
        // Periodicky zkusit osvěžit leg timery (u hnízda / u jídla), aby feromonová stopa nevyprchala nesprávně
        MaybeRefreshLegTimers();

        // Kladeni feromonů
        PlacePheromoneIfNeeded();

        // Náhodná složka řízení (explorace)
        HandleRandomSteering();

        // Řízení podle feromonů (vzorkování 3 senzorů – L/C/R)
        HandlePheromoneSteering(); // TODO: tady konec

        // Stav: buď hledám jídlo, nebo nesu domů
        if (mode == AntMode.ToFood) HandleFoodSeeking();
        if (mode == AntMode.ToHome) HandleReturnHome();

        // Vyhýbání překážkám (pseudo-antény + cooldown)
        HandleCollisionSteering();

        // Fyzikální integrace (reflexe/klouzání po zdi + posun/rotace)
        IntegrateMovement();
    }

    // Periodický refresh "leg timerů":
    //  - v ToFood: když jsem u hnízda, obnov "timeSinceLeftNest"
    //  - v ToHome: když nesu jídlo a blízko je jiné volné jídlo, obnov "timeSinceLeftFood"
    void MaybeRefreshLegTimers()
    {
        if (Time.time < nextLegRefreshTime) return;
        nextLegRefreshTime = Time.time + LegRefreshInterval;

        if (!sensorOrigin) sensorOrigin = transform;

        if (mode == AntMode.ToFood)
        {
            // Zjištění "jsem uvnitř hnízda?"
            Collider2D nestCol = nestRef.GetComponentInChildren<Collider2D>();
            bool atNest = nestCol.OverlapPoint(sensorOrigin.position);

            if (atNest)
            {
                // Reset leg času (domácí stopa znovu silná)
                timeSinceLeftNest = Time.time;
                lastPheromonePos  = transform.position;
            }
        }
        else if (mode == AntMode.ToHome)
        {
            // Když nesu jídlo, ale projíždím poblíž jiného dosud volného jídla, resetni "timeSinceLeftFood", ať stopa k jídlu nevyprchá
            float r = Mathf.Max(parameters.pickupDistance * 1.0f, 0.20f);

            int n = Physics2D.OverlapCircleNonAlloc(sensorOrigin.position, r, foodBuffer, foodLayer);
            for (int i = 0; i < n; i++)
            {
                var col = foodBuffer[i];
                if (!col) continue;

                if (col.GetComponentInParent<AntAgent>()) continue;      // ignoruj jídlo, které "nese" jiný agent
                if (col.TryGetComponent(out FoodItem fi) && fi.taken) continue; // Pro jistotu, v nejake milisekundě by se mohlo rozbít

                timeSinceLeftFood = Time.time; // refresh
                break;
            }
        }
    }

    // Po Updateu ještě vynutíme hranice herní plochy
    void LateUpdate() => EnforcePlayArea();
    #endregion

    // ─────────────────────────────────────────────────────────────────────────────
    // Integrovaný pohyb – skládání sil, kolize (reflexe/klouzání), posun/rotace
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Movement
    void IntegrateMovement()
    {
        // Finální "steer" je součet všech záměrů (random + feromony + cíl + vyhýbání)
        Vector2 steer = randomSteer + pheromoneSteer + targetSteer + obstacleAvoidForce;

        // Probíhá-li řízená otočka, přimícháme krátkodobý impuls
        if (isTurning)
        {
            steer += turnSteerForce * parameters.targetSteerStrength;
            if (Time.time > turnEndTime) isTurning = false;
        }

        // Přepočet na cílovou rychlost + plynulé přiblížení (acceleration)
        steer = steer.normalized;
        Vector2 dv = steer * parameters.maxSpeed;
        velocity = Vector2.Lerp(velocity, dv, parameters.acceleration * Time.deltaTime);

        float dt = Time.deltaTime;
        Vector2 move = velocity * dt;

        float r = Mathf.Max(parameters.collisionRadius, 0.05f); // fyzický poloměr agenta

        // Pokud jsem už uvnitř kolideru vytlačit ven po normále a začít klouzat po tečně
        Collider2D inside = Physics2D.OverlapCircle(transform.position, r * 0.98f, obstacleMask);
        if (inside)
        {
            Vector2 cp = inside.ClosestPoint(transform.position); // nejbližší bod na kolideru
            Vector2 n = (Vector2)transform.position - cp; // směrová normála ven
            n.Normalize();

            transform.position = cp + n * r; // posuň na bezpečný okraj
            SlideAlong(n); // a klouzej po stěně
            return;
        }

        // Předvídání nárazu
        RaycastHit2D hit = Physics2D.CircleCast(transform.position, r, heading, move.magnitude, obstacleMask);
        if (hit.collider)
        {
            Vector2 n = hit.normal;
            Vector2 posAtContact = hit.point + n * r;
            transform.position = posAtContact;

            SlideAlong(n); // klouzání po tečně
            return;
        }

        // Antény – když letím přímo do zdi, vyvolej otočku
        float probe = Mathf.Max(parameters.collisionRadius, Mathf.Max(parameters.antennaDistance, velocity.magnitude * dt));
        if (Physics2D.Raycast(transform.position, heading, probe, obstacleMask))
            StartTurnAround();

        // Běžný posun + natočení ve směru rychlosti
        transform.position += (Vector3)move;
        heading = velocity.normalized;
        transform.right = heading;
    }

    // Klouzání po stěně: projetí rychlosti do tečny, jemná náhoda, krátký cooldown vyhýbání
    void SlideAlong(Vector2 n)
    {
        Vector2 t = TangentFromNormal(n, velocity);                 // tečna z dané normály a preferovaného směru
        float speed = Mathf.Max(parameters.maxSpeed * 0.55f, velocity.magnitude * 0.65f);
        Vector2 vt = t.normalized * speed;

        velocity = vt;
        heading = vt.normalized;
        transform.right = heading;

        randomSteer += Perp(n) * 0.06f; // malý šum aby se trajektorie neupevnila příliš
        obstacleResetTime = Time.time + 0.10f; // po klouzání krátce ignoruj vyhýbání zpět do zdi
    }

    // Tečna ke stěně (normála n), tak aby byla co nejblíže aktuálnímu směru
    static Vector2 TangentFromNormal(Vector2 n, Vector2 prefer)
    {
        Vector2 t = new(-n.y, n.x);
        if (Vector2.Dot(t, prefer) < 0) t = -t;
        return t;
    }

    // Kolmice – užitečné pro anti-stuck kopanec
    static Vector2 Perp(Vector2 v) => new(-v.y, v.x);

    #endregion

    // ─────────────────────────────────────────────────────────────────────────────
    // Náhodné řízení – občas vygeneruj směr podobný současnému (explorace)
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Random Steering
    void HandleRandomSteering()
    {
        // Při aktivním cíli jídla náhodu vypni, aby se neodchyloval od jídla
        if (targetFood) { randomSteer = Vector2.zero; return; }

        // V definovaných intervalech hodit nový směr
        if (Time.time > nextRandomSteerTime)
        {
            ScheduleNextRandomSteer();
            
            // Vygenerujeme náhodný vektor, který ale bude nejvíc podobný našemu směru
            randomSteer = BestRandomDir(heading, 5) * parameters.randomSteerStrength;
        }
    }

    // Rozfázování času další změny náhodného směru
    void ScheduleNextRandomSteer() => nextRandomSteerTime = Time.time + Random.Range(parameters.randomSteerMaxDuration / 3f, parameters.randomSteerMaxDuration);

    // Vybere z několika náhodných vektorů ten, který je nejpodobnější aktuálnímu směru
    static Vector2 BestRandomDir(Vector2 refDir, int tries)
    {
        Vector2 best = Vector2.zero; 
        float bestDot = -1;
        for (int i = 0; i < tries; i++)
        {
            Vector2 r = Random.insideUnitCircle.normalized; // Náhodný vektor v rovině
            float d = Vector2.Dot(refDir, r); // cos skalárního součtu
            if (d > bestDot) // 1 = úplně stejný směr
            {
                bestDot = d; 
                best = r;
            }
        }
        return best;
    }
    #endregion

    // ─────────────────────────────────────────────────────────────────────────────
    // Kladeni feromonů – dle vzdálenosti od poslední kapky + doby od poslední nohy (leg timer)
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Pheromones
    void PlacePheromoneIfNeeded()
    {
        // Pokládej až po určité vzdálenosti
        if (Vector2.Distance(transform.position, lastPheromonePos) <= parameters.pheromoneSpacing)
            return;

        // Jak dlouho jsem na aktuální úseku (od hnízda / od jídla)?
        float legTime = (mode == AntMode.ToHome) ? Time.time - timeSinceLeftFood
                                                 : Time.time - timeSinceLeftNest;

        // Když dojde barvivo, už nepokládám
        if (parameters.pheromoneRunOutTime > 0 && legTime > parameters.pheromoneRunOutTime)
            return;

        // Síla kapky v čase slábne od 1 → 0.5 (lineárně v rámci runOutTime)
        float t = (parameters.pheromoneRunOutTime <= 0f) ? 1f : 1f - (legTime / parameters.pheromoneRunOutTime);
        float strength = Mathf.Lerp(0.5f, 1f, t);

        // Podle směru cesty klademe stopu do příslušného pole
        switch (mode)
        {
            case AntMode.ToFood:
                homeField?.Add(transform.position, strength);
                break;
            case AntMode.ToHome:
                foodField?.Add(transform.position, strength);
                break;
        }

        
        // Mírně náhodný offset pro přirozenější vzhled stopy
        lastPheromonePos = (Vector2)transform.position + Random.insideUnitCircle * parameters.pheromoneSpacing * 0.2f;
    }
    #endregion

    // ─────────────────────────────────────────────────────────────────────────────
    // Řízení podle feromonů – 3 senzory (L/C/R), vybere se nejbohatší směr
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Pheromone Steering
    void HandlePheromoneSteering()
    {
        // Throttling čtení senzorů
        if (Time.time < nextSensorSampleTime) return;
        nextSensorSampleTime = Time.time + parameters.timeBetweenSensorUpdate;

        // Vzorkovací úhly (L/C/R)
        float a = parameters.pheromoneSensorAngle;
        float L = SampleSensor(-a);
        float C = SampleSensor( 0);
        float R = SampleSensor( a);

        // Vyber směr s největší sílou a převeď na steer
        if (L > C && L > R) pheromoneSteer = Rotate(heading, -a) * parameters.steerStrength;
        else if (R > C) pheromoneSteer = Rotate(heading,  a) * parameters.steerStrength;
        else pheromoneSteer = heading * parameters.steerStrength;
    }

    // Odečte sílu feromonů v daném směru/pozici
    float SampleSensor(float angleDeg)
    {
        Vector2 dir  = Rotate(heading, angleDeg);
        Vector2 from = sensorOrigin.position;
        Vector2 pos  = from + dir * parameters.pheromoneSensorDistance;

        // Pokud je mezi senzorem a bodem zeď, čtení je nulové
        if (Physics2D.Linecast(from, pos, obstacleMask))
            return 0f;

        // Vyber správné pole podle režimu
        var field = (mode == AntMode.ToFood) ? foodField : homeField;
        return field.SampleStrength(pos, parameters.pheromoneSensorSize);
    }

    // Otočení vektoru o zadaný úhel (stupně)
    static Vector2 Rotate(Vector2 v, float ang) => (Quaternion.Euler(0, 0, ang) * v);
    #endregion

    // ─────────────────────────────────────────────────────────────────────────────
    // Stav HLEDÁNÍ JÍDLA – získání cíle a doladění steer k jídlu
    // ─────────────────────────────────────────────────────────────────────────────
    #region —  Food
    void HandleFoodSeeking()
    {
        // Najdi cíl, pokud žádný nemám
        if (!targetFood) AcquireTargetFood();

        // Když po hledání nic není, jdeme pryč
        if (!targetFood)
        {
            targetSteer = Vector2.zero; 
            return;
        }

        // Při cílení na jídlo vypni náhodu a feromony jen zjemni (ponecháno dle pův. chování)
        randomSteer = Vector2.zero;
        pheromoneSteer *= 0.2f;

        // Směr k cíli
        Vector2 toFood = (Vector2)targetFood.position - (Vector2)transform.position;
        targetSteer = toFood.normalized * (parameters.targetSteerStrength * 1.15f);

        // Ber nejbližší bod collideru 
        Vector2 probeFrom = sensorOrigin.position;
        Vector2 closest = targetFoodCol.ClosestPoint(probeFrom);
        float reach = parameters.pickupDistance + GetTargetRadius(targetFoodCol);

        if (Vector2.Distance(probeFrom, closest) <= reach)
            PickupFoodTarget();
    }

    // Výběr nejlepšího (nejbližšího) kandidáta jídla v dosahu – bez alokací
    void AcquireTargetFood()
    {
        int n = Physics2D.OverlapCircleNonAlloc(sensorOrigin.position, parameters.detectionRadius, foodBuffer, foodLayer);

        Collider2D best = null;
        float bestD2 = float.PositiveInfinity;

        for (int i = 0; i < n; i++)
        {
            var col = foodBuffer[i];

            // ignoruj jídlo jiných agentů
            if (col.GetComponentInParent<AntAgent>()) continue;

            // ignoruj sebrané
            if (col.TryGetComponent(out FoodItem fi) && fi.taken) continue;

            float d2 = ((Vector2)col.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (d2 < bestD2)
            {
                bestD2 = d2;
                best = col;
            }
        }

        targetFoodCol = best;
        targetFood = best ? best.transform : null;
    }

    // Odhad "poloměru" cíle pro robustní dosah (funguje i na necírkulární tvary)
    float GetTargetRadius(Collider2D col)
    {
        if (!col) return 0.05f;
        if (col is CircleCollider2D cc)
            return Mathf.Abs(cc.radius) * Mathf.Max(col.transform.lossyScale.x, col.transform.lossyScale.y);
        var e = col.bounds.extents;
        return Mathf.Max(0.03f, Mathf.Max(e.x, e.y));
    }

    // Samotný pickup – bezpečné odpojení fyziky, přichycení na "head", přepnutí režimu
    void PickupFoodTarget()
    {
        // Najdi kořenový FoodItem
        Transform root = targetFoodCol ? (targetFoodCol.GetComponentInParent<FoodItem>()?.transform ?? targetFoodCol.transform) : targetFood;

        // Zkus si jídlo vzít
        if (root && root.TryGetComponent(out FoodItem item))
        {
            if (!item.TryTake())
            {
                targetFood = null;
                targetFoodCol = null;
                return;
            }
        }

        // Přichycení k hlavě
        carriedItem = root;
        root.SetParent(head, true);
        root.localPosition = Vector3.zero;

        // Odstranění kolizí a fyziky u neseného předmětu
        root.gameObject.layer = 0;
        
        // Pro robustnost
        foreach (var c in root.GetComponentsInChildren<Collider2D>()) c.enabled = false;
        if (root.TryGetComponent<Rigidbody2D>(out var rb)) Destroy(rb);

        // Přepnutí režimu + refresh "od jídla"
        mode = AntMode.ToHome;
        timeSinceLeftFood = Time.time;
        targetFood = null;
        targetFoodCol = null;
        StartTurnAround();
    }

    // Stav NÁVRAT DOMŮ – když vidím hnízdo a není zeď
    void HandleReturnHome()
    {
        if (!nestRef)
        {
            targetSteer = Vector2.zero;
            return;
        }

        // Jsem uvnitř hnízda? Pokud ano, odevzdej a otoč se
        Collider2D nestCol = nestRef.GetComponentInChildren<Collider2D>();
        bool atNest = nestCol.OverlapPoint(sensorOrigin.position);
        if (atNest)
        {
            DepositFood();
            return;
        }

        targetSteer = Vector2.zero; // feromony necháme běžet, target bias je jen bonus

        // Přidej bias na hnízdo, pokud je v dosahu a cesta je bez překážky
        float r = parameters.detectionRadius;
        var nestInRange = Physics2D.OverlapCircle(sensorOrigin.position, r, nestLayer);
        if (nestInRange && !Physics2D.Linecast(sensorOrigin.position, (Vector2)nestRef.transform.position, obstacleMask))
        {
            Vector2 toNest = (Vector2)nestRef.transform.position - (Vector2)sensorOrigin.position;
            targetSteer = toNest.normalized * parameters.targetSteerStrength;
        }
    }

    // Odevzdání jídla – zničení itemu, inkrement skóre týmu, přepnutí do ToFood
    void DepositFood()
    {
        if (carriedItem) Destroy(carriedItem.gameObject);
        carriedItem = null;
        nestRef.ReportFood();

        mode = AntMode.ToFood;
        timeSinceLeftNest = Time.time;
        StartTurnAround();
    }
    #endregion

    // ─────────────────────────────────────────────────────────────────────────────
    // Vyhýbání překážkám – antény vlevo/vpravo a krátký cooldown na sílu vyhýbání
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Antennas + obstacles
    void HandleCollisionSteering()
    {
        // Po cooldownu nuluj sílu vyhýbání a stav "naposledy zasažené antény"
        if (Time.time > obstacleResetTime)
        {
            obstacleAvoidForce = Vector2.zero;
            lastAntennaHit = Antenna.None;
        }

        // Vektor "vlevo od směru" – základ pro offset antén
        Vector2 side = new(-heading.y, heading.x);

        // Původy levého/pravého čidla
        Vector2 leftOrigin  = (Vector2)sensorOrigin.position - side * parameters.antennaOffset;
        Vector2 rightOrigin = (Vector2)sensorOrigin.position + side * parameters.antennaOffset;

        // Raycasty dopředu pro obě antény
        RaycastHit2D hitL = Physics2D.Raycast(leftOrigin,  heading, parameters.antennaDistance, obstacleMask);
        RaycastHit2D hitR = Physics2D.Raycast(rightOrigin, heading, parameters.antennaDistance, obstacleMask);

        // Zasažením antény vznikne síla kolmo do směru, preferujeme kratší zásah (bližší překážku), zamezujeme okamžitému přeskakování stran
        if (hitL || hitR)
        {
            if (hitL && (lastAntennaHit != Antenna.Right) && (!hitR || hitL.distance < hitR.distance))
            {
                obstacleAvoidForce =  side * parameters.collisionAvoidSteerStrength;
                lastAntennaHit = Antenna.Left;
            }

            if (hitR && (lastAntennaHit != Antenna.Left) && (!hitL || hitR.distance < hitL.distance))
            {
                obstacleAvoidForce = -side * parameters.collisionAvoidSteerStrength;
                lastAntennaHit = Antenna.Right;
            }

            obstacleResetTime = Time.time + 0.35f;  // po chvíli vyprchá
            randomSteer = obstacleAvoidForce.normalized * parameters.randomSteerStrength; // trochu náhody ve stejném směru
        }
    }

    // Krátká řízená otočka (používá se při nárazu nebo po odevzdání)
    void StartTurnAround()
    {
        isTurning = true;
        turnEndTime = Time.time + 1.1f;

        Vector2 baseDir = -heading; // směr základní otočky
        Vector2 side = new(-baseDir.y, baseDir.x); // malý náhodný příčný rozptyl
        turnSteerForce = baseDir + side * (Random.value - .5f) * .4f; // [-0,2, 0,2]
    }
    #endregion

    // ─────────────────────────────────────────────────────────────────────────────
    // Udržení v herní oblasti – pokud jsem mimo, nacvakni mě zpět a otoč ke středu
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Play Area
    void EnforcePlayArea()
    {
        if (!playArea) return;
        var rect = playArea.GetWorldRect();

        if (rect.Contains(transform.position)) return;

        Vector2 clamped = playArea.ClampToArea(transform.position); // ořez na hranice
        transform.position = clamped;

        heading = ((Vector2)rect.center - clamped).normalized; // otoč směrem do středu
        StartTurnAround();
    }
    #endregion

    // ─────────────────────────────────────────────────────────────────────────────
    // Úklid – odstranit agenta z týmu
    // ─────────────────────────────────────────────────────────────────────────────
    void OnDestroy()
    {
        if (myTeamId >= 0)
            TeamManager.Instance?.UnregisterAnt(myTeamId);
    }
}
