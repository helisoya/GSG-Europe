using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Leguar.TotalJSON;

public class Parser : MonoBehaviour
{

    public static Dictionary<string, Vector3> ParsePoints()
    {

        string json = Resources.Load<TextAsset>("JSON/points").text;

        JSON obj = JSON.ParseString(json);

        JArray array = obj.GetJArray("points");
        Dictionary<string, Vector3> list = new Dictionary<string, Vector3>();

        for (int i = 0; i < array.Length; i++)
        {
            JSON vec = array.GetJSON(i);
            list.Add(vec.GetString("name"), new Vector3(vec.GetFloat("x"), vec.GetFloat("y"), vec.GetFloat("z")));
        }
        return list;
    }


    public static Dictionary<string, Province> ParseProvinces()
    {
        Dictionary<string, Vector3> points = ParsePoints();

        string json = Resources.Load<TextAsset>("JSON/provinces").text;

        JSON obj = JSON.ParseString(json);

        Dictionary<string, Province> list = new Dictionary<string, Province>();

        JArray array = obj.GetJArray("provinces");
        JArray arrayVertices;
        JSON jsonProv;
        Province province;
        Vector3[] vecs;
        for (int i = 0; i < array.Length; i++)
        {
            jsonProv = array.GetJSON(i);
            province = Instantiate(Manager.instance.prefabProvince, Manager.instance.provinceParent).GetComponent<Province>();
            province.gameObject.name = jsonProv.GetString("name");
            province.Province_Name = jsonProv.GetString("name");
            province.id = jsonProv.GetString("id");
            arrayVertices = jsonProv.GetJArray("vertices");

            vecs = new Vector3[arrayVertices.Length];

            for (int j = 0; j < arrayVertices.Length; j++)
            {
                vecs[j] = points[arrayVertices.GetString(j)];
            }
            province.Init(vecs);
            list.Add(province.id, province);
        }

        for (int i = 0; i < array.Length; i++)
        {
            jsonProv = array.GetJSON(i);

            string id = jsonProv.GetString("id");

            arrayVertices = jsonProv.GetJArray("adjacencies");
            list[id].adjacencies = new Province[arrayVertices.Length];

            for (int j = 0; j < arrayVertices.Length; j++)
            {
                if (!list.ContainsKey(arrayVertices.GetString(j)))
                {
                    print("Error : " + arrayVertices.GetString(j));
                    continue;
                }
                list[id].adjacencies[j] = list[arrayVertices.GetString(j)];
            }
        }

        return list;
    }


    public static Dictionary<string, GameEvent> ParseEvents()
    {
        string json = Resources.Load<TextAsset>("JSON/events").text;

        JSON obj = JSON.ParseString(json);

        Dictionary<string, GameEvent> list = new Dictionary<string, GameEvent>();

        JArray array = obj.GetJArray("events");
        JSON jsonEvent;
        JArray arrayIn;
        JArray arrayEffects;
        for (int i = 0; i < array.Length; i++)
        {
            jsonEvent = array.GetJSON(i);
            GameEvent gameEvent = new GameEvent();
            gameEvent.id = jsonEvent.GetString("id");
            gameEvent.title = jsonEvent.GetString("title");
            gameEvent.description = jsonEvent.GetString("description");


            arrayIn = jsonEvent.GetJArray("buttons");
            for (int j = 0; j < arrayIn.Length; j++)
            {
                JSON button = arrayIn.GetJSON(j);
                gameEvent.buttons[j] = new GameEvent.EventButton();
                gameEvent.buttons[j].label = button.GetString("name");
                arrayEffects = button.GetJArray("effect");
                gameEvent.buttons[j].effects = new string[arrayEffects.Length];
                for (int y = 0; y < arrayEffects.Length; y++)
                {
                    gameEvent.buttons[j].effects[y] = arrayEffects.GetString(y);
                }
            }


            list.Add(gameEvent.id, gameEvent);
        }
        return list;
    }

    public static Dictionary<string, Culture> ParseCultures(Dictionary<string, Dictionary<string, Focus>> focuses)
    {
        string json = Resources.Load<TextAsset>("JSON/cultures").text;

        JSON obj = JSON.ParseString(json);

        Dictionary<string, Culture> list = new Dictionary<string, Culture>();

        JArray array = obj.GetJArray("cultures");
        JSON jsonCulture;
        JArray arrayIn;
        for (int i = 0; i < array.Length; i++)
        {
            jsonCulture = array.GetJSON(i);
            Culture culture = new Culture();
            culture.id = jsonCulture.GetString("id");

            culture.prefabTank = Resources.Load<GameObject>("Units/" + culture.id);

            culture.focusTree = focuses[jsonCulture.GetString("focus")];

            arrayIn = jsonCulture.GetJArray("names");
            culture.prenoms = new string[arrayIn.Length];
            for (int j = 0; j < arrayIn.Length; j++)
            {
                culture.prenoms[j] = arrayIn.GetString(j);
            }

            arrayIn = jsonCulture.GetJArray("surnames");
            culture.noms = new string[arrayIn.Length];
            for (int j = 0; j < arrayIn.Length; j++)
            {
                culture.noms[j] = arrayIn.GetString(j);
            }

            list.Add(culture.id, culture);
        }
        return list;
    }

    public static Dictionary<string, Pays> ParsePays(Dictionary<string, Culture> cultures)
    {

        string json = Resources.Load<TextAsset>("JSON/pays").text;

        JSON obj = JSON.ParseString(json);

        Dictionary<string, Pays> list = new Dictionary<string, Pays>();

        JArray array = obj.GetJArray("pays");
        JSON jsonPays;
        JArray color;
        for (int i = 0; i < array.Length; i++)
        {
            jsonPays = array.GetJSON(i);
            Pays pays = new Pays();
            pays.ID = jsonPays.GetString("id");
            pays.cosmeticID = jsonPays.GetString("id");
            pays.nom = jsonPays.GetString("name");
            pays.culture = cultures[jsonPays.GetString("culture")];
            pays.DestroyIfNotSelected = jsonPays.GetString("secretNation").Equals("True");
            color = jsonPays.GetJArray("color");
            pays.SetColor(new Color(color.GetFloat(0) / 255f, color.GetFloat(1) / 255f, color.GetFloat(2) / 255f));
            list.Add(pays.ID, pays);
        }

        int comparison;
        foreach (Pays pays in list.Values)
        {
            foreach (Pays other in list.Values)
            {
                comparison = pays.ID.CompareTo(other.ID);
                if (comparison == 0) continue;

                if (other.relations.ContainsKey(pays.ID))
                {
                    pays.relations.Add(other.ID, other.relations[pays.ID]);
                }
                else
                {
                    pays.relations.Add(other.ID, new Relation(pays, other));
                }
            }
        }
        return list;
    }

    public static void ParseHistory(Dictionary<string, Province> allProvinces, Dictionary<string, Pays> allCountries, string fileName)
    {

        string json = Resources.Load<TextAsset>("JSON/" + fileName).text;

        JSON obj = JSON.ParseString(json);

        JArray array = obj.GetJArray("pays");
        JSON jsonPays;
        JArray arrayParties;
        JArray arrayProvinces;
        JSON party;
        for (int i = 0; i < array.Length; i++)
        {
            jsonPays = array.GetJSON(i);
            Pays pays = allCountries[jsonPays.GetString("id")];
            pays.Government_Form = jsonPays.GetInt("governement");

            arrayParties = jsonPays.GetJArray("parties");
            pays.parties = new Party[arrayParties.Length];
            for (int j = 0; j < arrayParties.Length; j++)
            {
                party = arrayParties.GetJSON(j);
                pays.parties[j] = new Party(party.GetString("name"), party.GetFloat("popularity"));
            }

            arrayProvinces = jsonPays.GetJArray("provinces");
            pays.provinces = new List<Province>();
            for (int j = 0; j < arrayProvinces.Length; j++)
            {
                pays.AddProvince(allProvinces[arrayProvinces.GetString(j)], false);
            }

            arrayProvinces = jsonPays.GetJArray("cores");
            pays.cores = new List<Province>();
            for (int j = 0; j < arrayProvinces.Length; j++)
            {
                pays.cores.Add(allProvinces[arrayProvinces.GetString(j)]);
            }
            pays.Reset_Flag();
            pays.Reset_Elections();
            pays.RefreshProvinces();
        }
        foreach (Pays pays in allCountries.Values)
        {
            pays.CheckNeighboors();
        }
    }

    public static List<FormableNation> ParseFormables(Dictionary<string, Province> allProvinces)
    {

        string json = Resources.Load<TextAsset>("JSON/formables").text;

        JSON obj = JSON.ParseString(json);

        List<FormableNation> list = new List<FormableNation>();

        JArray array = obj.GetJArray("formables");
        JSON formableJson;
        JArray arrayContestants;
        JArray arrayRequired;
        for (int i = 0; i < array.Length; i++)
        {
            formableJson = array.GetJSON(i);
            FormableNation formable = new FormableNation();
            formable.id = formableJson.GetString("id");
            formable.Name = formableJson.GetString("name");

            arrayContestants = formableJson.GetJArray("contestants");
            formable.contestants = new List<string>();
            for (int j = 0; j < arrayContestants.Length; j++)
            {
                formable.contestants.Add(arrayContestants.GetString(j));
            }

            arrayRequired = formableJson.GetJArray("required");
            formable.required = new List<Province>();
            for (int j = 0; j < arrayRequired.Length; j++)
            {
                formable.required.Add(allProvinces[arrayRequired.GetString(j)]);
            }

            list.Add(formable);
        }
        return list;
    }

    public static Dictionary<string, Dictionary<string, Focus>> ParseFocus()
    {
        Dictionary<string, Dictionary<string, Focus>> res = new Dictionary<string, Dictionary<string, Focus>>();
        TextAsset[] textAssets = Resources.LoadAll<TextAsset>("JSON/Focuses");

        foreach (TextAsset asset in textAssets)
        {
            string json = asset.text;

            JSON obj = JSON.ParseString(json);

            Dictionary<string, Focus> list = new Dictionary<string, Focus>();

            JArray array = obj.GetJArray("focus");
            JSON focusJSON;
            JArray arrayExclusive;
            JArray arrayRequired;
            for (int i = 0; i < array.Length; i++)
            {
                focusJSON = array.GetJSON(i);
                Focus focus = new Focus();
                focus.id = focusJSON.GetString("id");
                focus.focusName = focusJSON.GetString("name");
                focus.desc = focusJSON.GetString("desc");
                focus.requireAll = focusJSON.GetString("requireAll").Equals("True");
                focus.x = focusJSON.GetInt("x");
                focus.y = focusJSON.GetInt("y");

                arrayRequired = focusJSON.GetJArray("required");
                focus.required = new List<string>();
                for (int j = 0; j < arrayRequired.Length; j++)
                {
                    focus.required.Add(arrayRequired.GetString(j));
                }

                arrayExclusive = focusJSON.GetJArray("exclusive");
                focus.exclusive = new List<string>();
                for (int j = 0; j < arrayExclusive.Length; j++)
                {
                    focus.exclusive.Add(arrayExclusive.GetString(j));
                }


                focus.effect = new List<string>();
                arrayExclusive = focusJSON.GetJArray("effect");
                for (int j = 0; j < arrayExclusive.Length; j++)
                {
                    focus.effect.Add(arrayExclusive.GetString(j));
                }


                list.Add(focus.id, focus);
            }
            res.Add(obj.GetString("id"), list);
        }


        return res;
    }
}

