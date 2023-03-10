using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Province : MonoBehaviour
{

    public Pays controller = null;
    public Pays owner = null;
    public string Province_Name;
    public string id;


    private CanvasWorker canvas;

    public Vector3 center;

    private bool hover;

    public Province[] adjacencies;

    public bool hasRailroad;

    public void SetOwner(Pays pays)
    {
        owner = pays;
    }

    public void SetController(Pays pays)
    {
        controller = pays;
    }

    public void ComputeCenter(Vector3[] vecs)
    {
        center = Vector3.zero;
        center.y = vecs[0].y;

        float x = 0, y = 0, area = 0, k;
        Vector3 a, b = vecs[vecs.Length - 1];

        for (int i = 0; i < vecs.Length; i++)
        {
            a = vecs[i];

            k = a.z * b.x - a.x * b.z;
            area += k;
            x += (a.x + b.x) * k;
            y += (a.z + b.z) * k;

            b = a;
        }
        area *= 3;

        center.x = x / area;
        center.z = y / area;
    }

    public void SpawnUnitAtCity()
    {
        Vector3 pos = (Vector3)Random.insideUnitCircle * 2;
        pos.z = pos.y;
        owner.CreateUnit(pos + center);
    }

    public void Click_Event()
    {
        if (Manager.instance.picked)
        {
            CanvasWorker.instance.ShowBuyUnit(this);
        }
    }

    public void Init(Vector3[] vecs)
    {
        hover = false;
        ComputeCenter(vecs);


        canvas = CanvasWorker.instance;

        // Initialise la creation de la province en polygone

        GetComponent<MeshFilter>().mesh = MeshGenerator.GenerateMesh(vecs);

        gameObject.AddComponent(typeof(MeshCollider));

    }


    public void SetColor(Color col1, Color col2)
    {
        if (hover)
        {
            col1 = new Color(
            Mathf.Clamp(col1.r + 0.1f, 0, 1),
            Mathf.Clamp(col1.g + 0.1f, 0, 1),
            Mathf.Clamp(col1.b + 0.1f, 0, 1));

            col2 = new Color(
            Mathf.Clamp(col2.r + 0.1f, 0, 1),
            Mathf.Clamp(col2.g + 0.1f, 0, 1),
            Mathf.Clamp(col2.b + 0.1f, 0, 1));
        }
        GetComponent<Renderer>().material.SetColor("_Color1", col1);
        GetComponent<Renderer>().material.SetColor("_Color2", col2);
    }


    public void RefreshColor()
    {
        Manager manager = Manager.instance;
        if (manager.inPeaceDeal)
        {
            if (owner.ID != manager.peaceDealSide1 && owner.ID != manager.peaceDealSide2)
            {
                SetColor(MapModes.colors_grayed, MapModes.colors_grayed);
            }
            else if (manager.provincesToBeTakenInPeaceDeal.Contains(this))
            {
                SetColor(controller.color, controller.color);
            }
            else
            {
                SetColor(owner.color, controller.color);
            }
            return;
        }

        if (MapModes.currentMapMode == MapModes.MAPMODE.FEDERATION)
        {
            SetColor(
                owner.federation != null ? owner.federation.color : MapModes.colors_grayed,
                controller.federation != null ? controller.federation.color : MapModes.colors_grayed
                );
            return;
        }



        if (MapModes.currentMapMode == MapModes.MAPMODE.POLITICAL)
        {
            SetColor(owner.color, controller.color);
            return;
        }


        int indexOwner = 0;
        int indexController = 0;

        if (MapModes.currentMapMode == MapModes.MAPMODE.IDEOLOGICAL)
        {
            SetColor(MapModes.colors_ideologie[owner.Ideologie()], MapModes.colors_ideologie[controller.Ideologie()]);
        }
        else if (MapModes.currentMapMode == MapModes.MAPMODE.DIPLOMATIC)
        {
            if (owner == manager.player)
            {
                indexOwner = 4;
            }
            else
            {
                indexOwner = DetermineRelationToPlayer(owner);
            }

            if (controller == manager.player)
            {
                indexController = 4;
            }
            else
            {
                indexController = DetermineRelationToPlayer(controller);
            }

            Color ColOwner = MapModes.colors_relations[indexOwner];
            Color ColController = MapModes.colors_relations[indexController];
            if (indexOwner == 0)
            {
                int score = owner.relations[manager.player.ID].relationScore;
                ColOwner = Color.Lerp(Color.red, Color.green, (score + 100) / 200f);
            }
            if (indexController == 0)
            {
                int score = controller.relations[manager.player.ID].relationScore;
                ColController = Color.Lerp(Color.red, Color.green, (score + 100) / 200f);
            }
            SetColor(ColOwner, ColController);

        }
        else if (MapModes.currentMapMode == MapModes.MAPMODE.FORMABLE)
        {
            if (!manager.currentFormable.required.Contains(this))
            {
                SetColor(MapModes.colors_grayed, MapModes.colors_grayed);
                return;
            }

            indexOwner = owner == manager.player ? 0 : 1;
            indexController = controller == manager.player ? 0 : 1;
            SetColor(MapModes.colors_formable[indexOwner], MapModes.colors_formable[indexController]);
        }
    }


    int DetermineRelationToPlayer(Pays p)
    {
        if (p.lord == Manager.instance.player) return 2;
        if (Manager.instance.player.lord == p) return 3;
        if (p.relations[Manager.instance.player.ID].atWar) return 1;
        return 0;
    }

    void OnMouseDown()
    {
        if (!UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            if (Manager.instance.inPeaceDeal && Manager.instance.peaceDealSide2 == owner.ID && Manager.instance.player == controller)
            {
                CanvasWorker.instance.PeaceDealProvinceSelection(this);
                return;
            }


            if (Manager.instance.picked)
            {
                CanvasWorker.instance.ShowBuyUnit(this);
                canvas.Show_CountryInfo(owner);
            }
            else
            {
                CountryPicker.instance.UpdateCountry(owner);
            }



        }

    }

    void OnMouseEnter()
    {

        if (Manager.instance.isLoading || UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        hover = true;
        RefreshColor();
    }

    void OnMouseExit()
    {
        if (Manager.instance.isLoading) return;
        hover = false;
        RefreshColor();
    }

}