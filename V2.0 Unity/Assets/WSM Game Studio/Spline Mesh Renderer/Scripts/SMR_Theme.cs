using UnityEngine;

[CreateAssetMenu(fileName = "New Theme", menuName = "WSM Game Studio/Spline Mesh Renderer/Theme", order = 1)]
public class SMR_Theme : ScriptableObject
{
    public bool supportsVerticalBuilding = true;
    public bool zoomOnClick = true;
    public int zoomLevel = 20;
    public Color splineColor = Color.white;
    public bool showShortcutMenu = true;

    public int buttonSize = 50;

    public Texture AddIcon;
    public Texture DeleteIcon;
    public Texture ArrowUpIcon;
    public Texture CurvedArrowUpIcon;
    public Texture CurvedArrowDownIcon;
    public Texture CurvedArrowLeftIcon;
    public Texture CurvedArrowRightIcon;
    public Texture MeshIcon;
    public Texture LogIcon;

    public Texture SmallAddIcon;
    public Texture SmallDeleteIcon;
    public Texture SmallArrowUpIcon;
    public Texture SmallCurvedArrowUpIcon;
    public Texture SmallCurvedArrowDownIcon;
    public Texture SmallCurvedArrowLeftIcon;
    public Texture SmallCurvedArrowRightIcon;
    public Texture SmallMeshIcon;
    public Texture SmallLogIcon;

    public int inputBoxPosX = 10;
    public int inputBoxPosY = 10;

    public int button1PosX = 10;
    public int button1PosY = 80;

    public int button2PosX = 70;
    public int button2PosY = 80;

    public int button3PosX = 10;
    public int button3PosY = 140;

    public int button4PosX = 70;
    public int button4PosY = 140;

    public int button5PosX = 10;
    public int button5PosY = 200;

    public int button6PosX = 70;
    public int button6PosY = 200;

    public int button7PosX = 10;
    public int button7PosY = 260;

    public int button8PosX = 70;
    public int button8PosY = 260;
}
