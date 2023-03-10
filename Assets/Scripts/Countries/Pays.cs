using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pays
{

    public string ID = "000";

    public Color32 color;

    public string nom;

    public Culture culture;


    public List<Province> provinces;

    public int Government_Form = 0;
    public int currentParty = 2;

    public Leader leader;

    public int current_flag; // Drapeaus + Selection√©

    private static string[] flag_names = {
        "republic",
        "monarchy",
        "communism",
        "fascism"
    };

    public string cosmeticID;

    public Sprite currentFlag
    {
        get
        {
            Sprite result = Resources.Load<Sprite>("Flags/" + cosmeticID + "_" + flag_names[current_flag]);
            if (result != null) return result;
            return Resources.Load<Sprite>("Flags/" + cosmeticID);
        }
    }

    public Party[] parties;

    public int AP = 100;
    public int AP_PerMonth { get { return 10 + bonusAP + (federation != null ? 10 * federation.APBonus : 0); } }

    public int DP = 10;
    public int DP_PerMonth { get { return 2 + bonusDP + (federation != null ? federation.DPBonus : 0); } }

    private int bonusMilCap = 0;
    private int bonusAP = 0;
    private int bonusDP = 0;

    public int unitCap { get { return 5 + (provinces.Count / 2) + bonusMilCap + (federation != null ? 3 * federation.UCBonus : 0); } }


    public int date_elections = -1;
    private Manager manager;
    private Events events;
    public bool reelected = false;

    public List<Unit> units;

    public int unit_damage { get { return 10 + bonusDamage; } }

    public int unit_hp { get { return 100 + bonusHP; } }

    public int unit_evasion { get { return 20 + bonusEvasion; } }

    public int unit_speed { get { return 5 + bonusSpeed; } }

    public int unit_navalDamage { get { return unit_damage / 2 + unit_damage * (bonusNaval / 100); } }

    public int bonusNaval;
    public int bonusDamage;
    public int bonusHP;
    public int bonusEvasion;
    public int bonusSpeed;

    public bool hasTech_Naval = false;


    public Pays lord;
    public Federation federation;

    public Dictionary<string, Relation> relations;

    public List<Province> cores;
    public Dictionary<string, int> atWarWith;


    public List<string> focusDone;
    public string currentFocus;
    public int maxFocusTime;
    public int currentFocusTime;
    public bool DestroyIfNotSelected = false;


    public List<string> AI_MARKFORWAR;
    public List<Pays> AI_NEIGHBOORS;


    public bool canBuyUnit
    {
        get { return units.Count < unitCap; }
    }

    public Dictionary<string, Focus> focusTree
    {
        get { return culture.focusTree; }
    }


    public Pays()
    {
        maxFocusTime = 15;
        currentFocus = "NONE";
        focusDone = new List<string>();
        atWarWith = new Dictionary<string, int>();
        units = new List<Unit>();
        leader = new Leader();
        relations = new Dictionary<string, Relation>();
        manager = Manager.instance;
        events = manager.GetComponent<Events>();

        AI_MARKFORWAR = new List<string>();
        AI_NEIGHBOORS = new List<Pays>();

        Reset_Elections();
        Reset_Flag();
    }

    public void IncrementFocus()
    {
        if (currentFocus.Equals("NONE")) return;

        currentFocusTime--;
        if (currentFocusTime <= 0)
        {
            focusDone.Add(currentFocus);

            foreach (string effectNotSplited in focusTree[currentFocus].effect)
            {
                string[] effect = effectNotSplited.Split("(");
                effect[1] = effect[1].Split(")")[0];
                switch (effect[0])
                {
                    case "AP":
                        AP += int.Parse(effect[1], System.Globalization.NumberStyles.Any);
                        break;
                    case "APBONUS":
                        bonusAP += int.Parse(effect[1], System.Globalization.NumberStyles.Any);
                        break;
                    case "DP":
                        DP += int.Parse(effect[1], System.Globalization.NumberStyles.Any);
                        break;
                    case "DPBONUS":
                        bonusDP += int.Parse(effect[1], System.Globalization.NumberStyles.Any);
                        break;
                    case "SETGOVERNEMENT":
                        Government_Form = int.Parse(effect[1], System.Globalization.NumberStyles.Any);
                        Reset_Elections();
                        Reset_Flag();
                        break;
                    case "MILCAP":
                        bonusMilCap += int.Parse(effect[1], System.Globalization.NumberStyles.Any);
                        break;
                    case "SPEED":
                        bonusSpeed += int.Parse(effect[1], System.Globalization.NumberStyles.Any);
                        break;
                    case "HP":
                        bonusHP += int.Parse(effect[1], System.Globalization.NumberStyles.Any);
                        break;
                    case "EVASION":
                        bonusEvasion += int.Parse(effect[1], System.Globalization.NumberStyles.Any);
                        break;
                    case "ATTACK":
                        bonusDamage += int.Parse(effect[1], System.Globalization.NumberStyles.Any);
                        break;
                    case "ENABLE_NAVAL":
                        hasTech_Naval = true;
                        break;
                    case "PARTYPOP":
                        string[] split = effect[1].Split(",");
                        Add_Popularity(int.Parse(split[0], System.Globalization.NumberStyles.Any), int.Parse(split[1], System.Globalization.NumberStyles.Any));
                        break;
                    case "FREE":
                        Pays p = manager.GetCountry(effect[1]);
                        foreach (Province prov in p.cores)
                        {
                            if (provinces.Contains(prov))
                            {
                                RemoveProvince(prov);
                                p.AddProvince(prov);
                            }
                        }
                        break;
                    case "ANNEXPROVINCE":
                        string[] splited = effect[1].Split(",");
                        Pays p1 = manager.GetCountry(splited[0]);
                        Province prov1 = manager.GetProvince(splited[1]);
                        prov1.owner.RemoveProvince(prov1);
                        p1.AddProvince(prov1);
                        break;
                    case "COSMETIC":
                        cosmeticID = effect[1];
                        break;
                    case "PUPPET":
                        Pays p2 = manager.GetCountry(effect[1]);
                        CanvasWorker.instance.UpdateRelations_ShortCut(p2, this, 3);
                        break;
                }
            }
            currentFocus = "NONE";

            if (this == manager.player)
            {
                CanvasWorker.instance.UpdateFocus();
            }
        }
    }



    public bool PrerequistDone(Focus focus)
    {
        if (focus.required.Count == 0) return true;

        foreach (string exlcusive in focus.exclusive)
        {
            if (focusDone.Contains(exlcusive)) return false;
        }

        foreach (string prerequist in focus.required)
        {
            if (!focusDone.Contains(prerequist) && focus.requireAll) return false;
            if (focusDone.Contains(prerequist) && !focus.requireAll) return true;
        }

        return focus.requireAll;
    }

    public List<Focus> GetAvailableFocus()
    {
        List<Focus> list = new List<Focus>();
        foreach (Focus focus in focusTree.Values)
        {
            if (focusDone.Contains(focus.id)) continue;
            if (!PrerequistDone(focus)) continue;
            list.Add(focus);
        }
        return list;
    }

    public void CutDownArmy()
    {
        if (units.Count <= unitCap) return;

        List<Unit> correctList = new List<Unit>();
        for (int i = 0; i < units.Count && i < unitCap; i++)
        {
            correctList.Add(units[i]);
        }

        for (int i = unitCap; i < units.Count; i++)
        {
            Manager.Destroy(units[i].gameObject);
        }

        units = correctList;
    }


    public void SetColor(Color col)
    {
        color = col;
    }


    public void UpdateUnitsSeen(bool value)
    {
        foreach (Unit obj in units)
        {
            obj.UpdateIsSeen(value);
        }
    }

    public void ChangeFocus(string newFocus)
    {
        currentFocus = newFocus;
        currentFocusTime = maxFocusTime;
    }

    public void DeclareWarOnCountry(Pays country)
    {
        if (relations[country.ID].atWar)
        {
            return;
        }
        relations[country.ID].atWar = true;
        relations[country.ID].wargoals.Remove(ID);

        atWarWith.Add(country.ID, 0);
        country.atWarWith.Add(ID, 0);
    }

    public void MakePeaceWithCountry(Pays other)
    {
        if (!relations[other.ID].atWar)
        {
            return;
        }

        relations[other.ID].atWar = false;
        relations[other.ID].relationScore = 0;

        atWarWith.Remove(other.ID);
        other.atWarWith.Remove(ID);

        Province[] copy = other.provinces.ToArray();

        foreach (Province p in copy)
        {
            if (p.controller.ID == ID)
            {
                AddProvince(p);
                other.RemoveProvince(p);
            }
        }
        other.CutDownArmy();
        CutDownArmy();

        if (this == manager.player)
        {
            CanvasWorker.instance.RefreshUtilityBar();
        }
    }


    public void RemoveUnitsFromCountry(Province placeTo, string country)
    {
        foreach (Unit unit in units)
        {
            if (unit.IsOnCountryTerritory(country))
            {
                unit.transform.position = placeTo.center;
            }
        }
    }

    public void RefreshProvinces()
    {
        for (int i = 0; i < provinces.Count; i++)
        {
            provinces[i].RefreshColor();
        }
    }

    public bool CompletelyOccupied()
    {
        foreach (Province province in provinces)
        {
            if (province.controller == this) return false;
        }
        return true;
    }


    public void NewYear()
    {
        if (leader.Age(1))
        { // Leader Mort (Trigger Elections / Selection Successeur)
            if (3 == Government_Form || Government_Form == 5)
            { // Monarchie
                SameFamilyLeader();
                if (manager.player == this)
                {
                    events.DeathLeader_Monarchy(ID);
                }
            }
            else
            { // Le Reste + Monarchie Elective
                RandomizeLeader();
                if (manager.player == this)
                {
                    events.DeathLeader_Normal(ID);
                }
            }
        }

        if (date_elections == manager.an)
        { // Trigger Elections
            TriggerElections();
        }
    }

    public void Add_Popularity(int index, float nb)
    {
        parties[index].popularity = Mathf.Clamp(parties[index].popularity + nb, 0, 100);

        int lg = 0;
        for (int i = 0; i < parties.Length; i++)
        {
            if (parties[i].popularity != 0 && i != index)
            {
                lg++;
            }
        }

        float other = nb / lg;
        for (int i = 0; i < parties.Length; i++)
        {
            if (i != index)
            {
                parties[i].popularity = Mathf.Clamp(parties[i].popularity - other, 0, 100);
            }
        }

        // Cas Gouvernement autoritaire

        if (index == 1 && parties[index].popularity > parties[4].popularity + parties[3].popularity && (Government_Form == 3 || Government_Form == 4 || Government_Form >= 9))
        {
            RandomizeLeader();
            Choix_Type(1); // Gauche Renverse le Pouvoir pour une r√©publique
        }
        else if (index == 0 && parties[index].popularity > parties[4].popularity + parties[3].popularity && (Government_Form == 3 || Government_Form == 4 || Government_Form >= 9))
        {
            RandomizeLeader();
            Choix_Type(3); // Extr. Gauche Renverse le Pouvoir pour un Soviet
        }
        else if (index == 3 && parties[index].popularity > parties[0].popularity + parties[1].popularity && (Government_Form == 6))
        {
            RandomizeLeader();
            Choix_Type(1); // Droite Renverse le Pouvoir pour une Republique
        }
        else if (index == 4 && parties[index].popularity > parties[0].popularity + parties[1].popularity && (Government_Form == 6))
        {
            RandomizeLeader();
            Choix_Type(4); // Extr. Droite Renverse le Pouvoir pour un Fascisme
        }
    }

    public void RandomizeLeader()
    {
        leader.nom = culture.GetRandom_Nom();
        leader.prenom = culture.GetRandom_Prenom();
        leader.age = Random.Range(30, 70);
        leader.RandomizeLeaderGFX();
        leader.ResetDeath();
    }

    public void SameFamilyLeader()
    {
        leader.prenom = culture.GetRandom_Prenom();
        leader.age = Random.Range(30, 70);
        leader.RandomizeLeaderGFX();
        leader.ResetDeath();
    }

    public void TriggerElections()
    {
        int index = 0;
        for (int i = 1; i < parties.Length; i++)
        {
            if (parties[i].popularity > parties[index].popularity)
            {
                index = i;
            }
        }
        Add_Popularity(index, 5);
        currentParty = index;

        if (manager.player == this)
        {
            events.Elections(ID, index);
        }
        else
        {
            if (index == 0 && Ideologie() != 2)
            { // Cas -> Communiste
                Government_Form = GetRandomGov(3);
                Reset_Flag();
                RandomizeLeader();
            }
            else if (index == 4 && Ideologie() != 3)
            { // Cas -> Fasciste
                Government_Form = GetRandomGov(4);
                Reset_Flag();
                RandomizeLeader();
            }
            else if ((index >= 2 && Ideologie() == 2) || (index <= 2 && Ideologie() == 3) || (index <= 1 && Ideologie() == 1))
            { // Cas -> Republique
                Government_Form = GetRandomGov(4);
                Reset_Flag();
                RandomizeLeader();
            }
            else if (reelected)
            { // Cas -> monarchie
                if (Random.Range(0, 100) < Random.Range(40, 60))
                {
                    reelected = false;
                    Government_Form = GetRandomGov(2);
                    Reset_Flag();
                }
                else
                {
                    RandomizeLeader();
                }
                reelected = false;
            }
            else
            { // Autre
                if (Random.Range(0, 100) < Random.Range(30, 70))
                {
                    reelected = true;
                }
                else
                {
                    RandomizeLeader();
                }
            }
        }

        Reset_Elections();
    }

    public void Reset_Elections()
    {
        if (Government_Form == 0)
        { // Set Prochaines Elections
            date_elections = manager.an + 4;
        }
        else if (Government_Form == 1)
        {
            date_elections = manager.an + 5;
        }
        else if (Government_Form == 2)
        {
            date_elections = manager.an + 7;
        }
        else if (Government_Form == 5)
        {
            date_elections = manager.an + 8;
        }
        else if (Government_Form == 7)
        {
            date_elections = manager.an + 10;
        }
        else if (Government_Form == 8)
        {
            date_elections = manager.an + 20;
        }
        else
        {
            date_elections = -1;
        }
    }

    public void Reset_Flag()
    {
        if (Government_Form <= 2)
        {
            current_flag = 0;
        }
        else if (Government_Form <= 5)
        {
            current_flag = 1;
        }
        else if (Government_Form <= 8)
        {
            current_flag = 2;
        }
        else
        {
            current_flag = 3;
        }
        foreach (Unit unit in units)
        {
            unit.UpdateFlag();
        }
    }

    int GetRandomGov(int type)
    {
        if (type == 1)
        {
            return Random.Range(0, 3);
        }
        else if (type == 2)
        {
            return Random.Range(3, 6);
        }
        else if (type == 3)
        {
            return Random.Range(6, 9);
        }
        return Random.Range(9, 12);
    }


    public void Choix_Type(int groupe)
    {

        if (this == manager.player)
        {
            if (groupe == 1)
            { // Republique
                events.ChoixType_Rep(ID);

            }
            else if (groupe == 2)
            { // Monarchie
                events.ChoixType_Mon(ID);

            }
            else if (groupe == 3)
            { // Communisme
                events.ChoixType_Com(ID);

            }
            else
            { // Fascisme
                events.ChoixType_Fas(ID);
            }
        }
        else
        {
            Government_Form = GetRandomGov(groupe);
            Reset_Elections();
            Reset_Flag();
        }
    }


    public void RemoveUnit(Unit unit)
    {
        units.Remove(unit);
    }

    public void CreateUnit(Vector3 pos)
    {
        Unit obj = Manager.Instantiate(culture.prefabTank, GameObject.Find("Units").transform).GetComponent<Unit>();
        pos.y = 0.3f;
        obj.transform.position = pos;
        obj.country = this;
        units.Add(obj);
        CanvasWorker.instance.RefreshUtilityBar();
    }




    public void RemoveProvince(Province prov)
    {
        provinces.Remove(prov);
        if (provinces.Count == 0)
        {
            foreach (Unit unit in units)
            {
                Manager.Destroy(unit.gameObject);
            }
        }
        CheckNeighboors();
    }

    public void AddProvince(Province prov, bool refresh = true)
    {
        provinces.Add(prov);
        prov.SetOwner(this);
        prov.SetController(this);
        if (refresh)
        {
            prov.RefreshColor();
        }
        CheckNeighboors();
    }

    public void CheckNeighboors()
    {
        AI_NEIGHBOORS.Clear();
        foreach (Province province in provinces)
        {
            foreach (Province neighboor in province.adjacencies)
            {
                if (neighboor.owner != this && !AI_NEIGHBOORS.Contains(neighboor.owner))
                {
                    AI_NEIGHBOORS.Add(neighboor.owner);
                }
            }
        }
    }


    public void CopyCat(Pays c)
    {
        Government_Form = c.Government_Form;
        Reset_Elections();
        Reset_Flag();
        RandomizeLeader();
    }

    public void MimicColor(Pays c)
    {
        color = Color32.Lerp(color, c.color, 0.6f);
        RefreshProvinces();
    }

    public int Ideologie()
    {
        if (Government_Form <= 2)
        {
            return 0;
        }
        else if (Government_Form <= 5)
        {
            return 1;
        }
        else if (Government_Form <= 8)
        {
            return 2;
        }
        return 3;
    }

}