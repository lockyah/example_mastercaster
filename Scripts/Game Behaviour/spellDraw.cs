using UnityEngine;
using System;
using System.Collections.Generic;
using TMPro;

using PDollarGestureRecognizer;
using Random = UnityEngine.Random;
using UnityEngine.UI;

public class spellDraw : MonoBehaviour
{
    private persistentManagerHelper pmh;
    private ParticleSystem createEffect;
    private GameObject[] castObjects;

    public bool canDraw = true; //False when paused
    public bool checkingSpells = false; //True when using Spells menu. Prevents pausing mid-check breaking the system.
    public bool identifiedShape = false; //True when successfully classified drawing
    public string className = ""; //Used to create the matching object. Same as the classification names, not the translations!
    public Transform drawLinePrefab;
    public float surenessValue = 0f; //What value does the match need to have to be allowed?

    private RuntimePlatform platform; //What device is the player using?
    private Vector3 penPosition = Vector2.zero;
    private Rect drawArea;
    private List<Gesture> shapeData = new List<Gesture>();

    private List<Point> points = new List<Point>();
    public int strokeId = -1;
    private int vertexCount = 0;
    private List<LineRenderer> gestureLinesRenderer = new List<LineRenderer>();
    private LineRenderer currentGestureLineRenderer;

    private float evaluateTime = 0f;
    public float evaluateCooldown = 0f; //How long should the game wait after the pen is raised to classify?
    private TMP_Text runeName;
    private Button castButton;

    // Start is called before the first frame update
    void Start()
    {
        platform = Application.platform;

        castObjects = Resources.LoadAll<GameObject>("Prefabs/Items/"); //Load prefabs of shapes into the game
        createEffect = Resources.Load<ParticleSystem>("Prefabs/Particle System/Create Item");

        castButton = GameObject.Find("Cast").GetComponent<Button>();
        runeName = castButton.transform.GetChild(1).GetComponent<TMP_Text>();

        pmh = GetComponent<persistentManagerHelper>();

        //Use screen proportions to define drawing area
        drawArea = new Rect(0, Screen.height/6.5f, Screen.width, Screen.height - Screen.height / 2.5f);

        //Read in spell shape data from resource folder
        TextAsset[] gesturesXml = Resources.LoadAll<TextAsset>("SpellShapes/");
        foreach (TextAsset gestureXml in gesturesXml)
        {
            shapeData.Add(GestureIO.ReadGestureFromXML(gestureXml.text));
        }

        clearDrawing();
    }


    private void Update()
    {
        if (canDraw && !checkingSpells)
        {
            //Only draw and evaluate if the player can actually draw!
            if (platform == RuntimePlatform.Android || platform == RuntimePlatform.IPhonePlayer)
            {
                if (Input.touchCount > 0)
                {
                    penPosition = new Vector3(Input.GetTouch(0).position.x, Input.GetTouch(0).position.y);
                }
            }
            else
            {
                if (Input.GetMouseButton(0))
                {
                    penPosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y);
                }
            }

            if (drawArea.Contains(penPosition))
            {
                //  ----    DRAWING SHAPE   ----
                //Player must be drawing in the defined area for it to count

                if (Input.GetMouseButtonDown(0))
                {
                    identifiedShape = false;
                    runeName.text = "";
                    castButton.interactable = false;

                    ++strokeId;

                    Transform tmpGesture = Instantiate(drawLinePrefab, transform.position, transform.rotation) as Transform;
                    currentGestureLineRenderer = tmpGesture.GetComponent<LineRenderer>();

                    gestureLinesRenderer.Add(currentGestureLineRenderer);

                    vertexCount = 0;
                }

                if (Input.GetMouseButton(0) && strokeId > -1)
                {
                    points.Add(new Point(penPosition.x, -penPosition.y, strokeId));

                    currentGestureLineRenderer.positionCount = ++vertexCount;
                    currentGestureLineRenderer.SetPosition(vertexCount - 1, Camera.main.ScreenToWorldPoint(new Vector3(penPosition.x, penPosition.y, 10)));
                }
            }

            //  ----    EVALUATING SHAPE   ----
            //Done outside of the draw area check since we always want to be checking for this.

            if (Time.timeScale > 0)
            {
                if (Input.GetMouseButtonUp(0) && gestureLinesRenderer.Count > 0)
                {
                    //On pen up, set a timer
                    evaluateTime = Time.time + evaluateCooldown;

                    //Check that the new line has distance (not a dot, which causes an error).
                    //If it doesn't, remove the last line and its points
                    if (currentGestureLineRenderer != null && Vector3.Distance(currentGestureLineRenderer.GetPosition(0), currentGestureLineRenderer.GetPosition((int)vertexCount/2)) < 0.1f)
                    {
                        strokeId--;

                        points.RemoveRange(points.Count - currentGestureLineRenderer.positionCount, currentGestureLineRenderer.positionCount);

                        gestureLinesRenderer.Remove(currentGestureLineRenderer);
                        Destroy(currentGestureLineRenderer.gameObject);
                    }

                }
                else if (!Input.GetMouseButton(0) && Time.time >= evaluateTime && strokeId > -1 && !identifiedShape)
                {
                    //Wait for user to stop drawing, then evaluate what they drew
                    evaluateDrawing();
                }
            }
        }
    }

    public void clearDrawing()
    {
        strokeId = -1;
        runeName.text = "";
        className = "";
        identifiedShape = false;
        castButton.interactable = false;

        points.Clear();

        foreach (LineRenderer lineRenderer in gestureLinesRenderer)
        {
            Destroy(lineRenderer.gameObject);
        }

        gestureLinesRenderer.Clear();
    }

    public void evaluateDrawing()
    {
        Gesture candidate = new Gesture(points.ToArray());
        Result gestureResult = PointCloudRecognizer.Classify(candidate, shapeData.ToArray());

        identifiedShape = true;
        if (gestureResult.Score >= surenessValue)
        {
            //Success, find shape-to-spell name from dictionary and make cast available
            String name = pmh.translateSpell(gestureResult.GestureClass);
            className = gestureResult.GestureClass;
            runeName.text = name;

            castButton.interactable = true;
        }
        else
        {
            runeName.text = "?";
        }
    }

    public void castRune()
    {

        if (castObjects.Length != 0)
        {
            foreach(GameObject go in castObjects)
            {
                if(go.name == className)
                {
                    //Find middle of drawn object to instantiate the ingredient to.
                    Vector3 insPos;
                    if (gestureLinesRenderer.Count == 1)
                    {
                        insPos = gestureLinesRenderer[0].bounds.center;
                    }
                    else
                    {
                        Bounds b = new Bounds(gestureLinesRenderer[0].bounds.center, Vector3.zero);
                        foreach (LineRenderer l in gestureLinesRenderer)
                        {
                            b.Encapsulate(l.bounds.center);
                        }
                        insPos = b.center;
                    }

                    //Create the object at this position with some pop force and clear everything out!
                    GameObject g = Instantiate(go);
                    g.name = className;
                    g.transform.position = insPos;
                    Instantiate(createEffect, g.transform).transform.parent = null;

                    Rigidbody2D rb = g.GetComponent<Rigidbody2D>();
                    rb.AddForce(new Vector2(Random.Range(-2f, 2f), 5f), ForceMode2D.Impulse);
                    rb.AddTorque(Random.Range(-20f, 20f)); ;

                    clearDrawing();
                    break;
                }
            }
        } else
        {
            Debug.Log("Error casting a rune - no spell prefabs were found in Resources.");
        }

    }    
}
