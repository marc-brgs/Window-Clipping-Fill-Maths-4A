using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;

    [SerializeField] private GameObject polygon;
    [SerializeField] private GameObject window;

    private LineRenderer lrPolygon;
    private LineRenderer lrWindow;

    private bool drawingPolygon;
    private bool drawingWindow;
    private int polygonIndex;
    private int windowIndex;

    [SerializeField] private GameObject textPolygon;
    [SerializeField] private GameObject textWindow;
    
    [SerializeField] private Image img;
    private Color fillColor;

    // Start is called before the first frame update
    void Start()
    {
        lrPolygon = polygon.GetComponent<LineRenderer>();
        lrWindow = window.GetComponent<LineRenderer>();
        drawingPolygon = false;
        drawingWindow = true;
        polygonIndex = 0;
        windowIndex = 0;
        
        img.gameObject.SetActive(false);
        clearTexture(img.sprite.texture);
        fillColor = Color.yellow;
    }
    
    // Update is called once per frame
    void Update()
    {
        if (drawingWindow)
            drawWindow();
        
        if (drawingPolygon)
            drawPolygon();

        if (Input.GetKeyDown(KeyCode.Delete))
        {
            lrPolygon.SetPositions(Array.Empty<Vector3>());
            lrPolygon.positionCount = 0;
            polygonIndex = 0;
            lrWindow.SetPositions(Array.Empty<Vector3>());
            lrWindow.positionCount = 0;
            windowIndex = 0;
            drawingPolygon = false;
            drawingWindow = true;
            textPolygon.SetActive(false);
            textWindow.SetActive(true);
            img.gameObject.SetActive(false);
            clearTexture(img.sprite.texture);
        }
    }
    
    public void ClipPolygon(int method) // 0 = cyrus beck | 1 = sutherland
    {
        if (drawingPolygon || drawingWindow) return; // Wait for fully drawn polygons
        Debug.Log("Clip");
        
        if(method == 0)
            CyrusBeck();
        else
            SutherlandHodgmann();
        
        clearTexture(img.sprite.texture);
    }

    public void CyrusBeck()
    {
        
    }
    
    /**
     * Clip polygon to window (polygon must be convex and drawn clockwise)
     */
    public void SutherlandHodgmann()
    {
        // Recover data
        int N1 = lrPolygon.positionCount;
        Vector3[] PL = new Vector3[N1];
        lrPolygon.GetPositions(PL);
        
        int N3 = lrWindow.positionCount;
        Vector3[] PW = new Vector3[N3];
        lrWindow.GetPositions(PW);

        Vector3 S = new Vector3();
        Vector3 F = new Vector3();
        Vector3 I; // point d'intersection
        
        int N2;
        List<Vector3> PS;

        for (int i = 0; i < N3-1; i++) // i = 1 dans l'algo
        {
            N2 = 0;
            PS = new List<Vector3>();

            for (int j = 0; j < N1; j++) // j = 1 dans l'algo
            {
                if (j == 0)
                    F = PL[j]; /* Sauver le premier = dernier sommet */
                else
                {
                    if(coupe(S, PL[j], PW[i], PW[i+1]))
                    {
                        I = intersection(S, PL[j], PW[i], PW[i + 1]);
                        PS.Add(I);
                        N2++;
                    }
                }

                S = PL[j];
                if(visible(S, PW[i], PW[i+1])) {
                    PS.Add(S);
                    N2++;
                }
            }

            if (N2 > 0)
            {
                /* Traitement du dernier côté de PL */
                if(coupe(S, F, PW[i], PW[i+1]))
                {
                    I = intersection(S, F, PW[i], PW[i + 1]);
                    PS.Add(I);
                    N2++;
                }
                
                /* Découpage pour chacun des polygones */
                lrPolygon.positionCount = N2;
                Vector3[] ArrayPS = PS.ToArray();
                lrPolygon.SetPositions(ArrayPS);
                
                /* Ferme le polygone si non fermé */
                if(ArrayPS[0] != ArrayPS[ArrayPS.Length - 1])
                {
                    lrPolygon.positionCount++;
                    lrPolygon.SetPosition(lrPolygon.positionCount-1, lrPolygon.GetPosition(0));
                }
                
                PL = ArrayPS;
                N1 = N2;
            }

            
        }
    }

    /**
     * Determine if 2 sides are intersecting
     * Used by Sutherland algorithm
     */
    private bool coupe(Vector3 a1, Vector3 a2, Vector3 b1, Vector3 b2)
    {
        Vector3 b = a2 - a1;
        Vector3 d = b2 - b1;
        float bDotDPerp = b.x * d.y - b.y * d.x;

        // if b dot d == 0, it means the lines are parallel so have infinite intersection points
        if (bDotDPerp == 0)
            return false;

        Vector3 c = b1 - a1;
        float t = (c.x * d.y - c.y * d.x) / bDotDPerp;
        if (t < 0 || t > 1)
            return false;

        float u = (c.x * b.y - c.y * b.x) / bDotDPerp;
        if (u < 0 || u > 1)
            return false;
        
        return true;
    }

    /**
     * Return the intersection point of 2 sides
     * Used by Sutherland algorithm
     */
    private Vector3 intersection(Vector3 a1, Vector3 a2, Vector3 b1, Vector3 b2)
    {
        Vector3 intersection;

        Vector3 b = a2 - a1;
        Vector3 d = b2 - b1;
        float bDotDPerp = b.x * d.y - b.y * d.x;

        Vector3 c = b1 - a1;
        float t = (c.x * d.y - c.y * d.x) / bDotDPerp;

        intersection = a1 + (b * t);
        return intersection;
    }
    
    /*
     * Determine if a point is inside of a side of polygon with clockwise normal
     * Used by Sutherland and RemplissageRectEG algorithms
     */
    private bool visible(Vector3 S, Vector3 F1, Vector3 F2)
    {
        Vector2 midToS = new Vector2(S.x - F1.x, S.y - F1.y);
        
        Vector2 n = new Vector2(-(F2.y - F1.y), F2.x - F1.x);
        Vector2 m = -n;
        
        if(Vector3.Dot(n, midToS) < 0) // dedans
            return true;
        if(Vector3.Dot(n, midToS) > 0) // dehors
            return false;
        // sur le bord de la fenêtre
        return true;
    }
    
    /*
     * Reset sprite texture used for filling to transparent
     */
    private void clearTexture(Texture2D tex)
    {
        for (int x = 0; x < tex.width; x++)
        {
            for (int y = 0; y < tex.height; y++)
            {
                tex.SetPixel(x, y, Color.clear);
            }
        }
        tex.Apply();
    }
    
    public void ChangeColor(int color=0)
    {
        switch (color)
        {
            case 0:
                fillColor = Color.yellow;
                break;
            case 1:
                fillColor = Color.magenta;
                break;
            case 2:
                fillColor = Color.green;
                break;
            case 3:
                fillColor = Color.cyan;
                break;
        }
        RemplissageRectEG();
    }
    
    // Remplissage RectEG
    public void RemplissageRectEG()
    {
        // Recover data
        Vector3[] Poly = new Vector3[lrPolygon.positionCount];
        lrPolygon.GetPositions(Poly);
        int nb = 0;
        Vector2[] rectEG = rectangleEnglobant(Poly);
        int xmin = (int) rectEG[0].x;
        int ymin = (int) rectEG[0].y;
        int xmax = (int) rectEG[1].x;
        int ymax = (int) rectEG[1].y;

        for (int x = xmin; x < xmax; x++)
        {
            for (int y = ymin; y < ymax; y++)
            {
                if (interieur(x, y, Poly))
                {
                    affichePixel(x, y);
                    nb++;
                }
            }
        }
        
        img.sprite.texture.Apply();
        img.gameObject.SetActive(true);
    }

    /*
     * Used by RemplissageRectEG to determine x y min max for pixel loop optimization
     */
    private Vector2[] rectangleEnglobant(Vector3[] Poly)
    {
        int xmin = Screen.width, xmax = 0, ymin = Screen.height, ymax = 0;
        
        for (int i = 0; i < Poly.Length; i++)
        {
            Vector2 polyPixel = new Vector2(worldPosXToPixel(Poly[i].x), worldPosYToPixel(Poly[i].y));
            Debug.Log("worldpos: " + Poly[i].x + " " + Poly[i].y + ", pixel: " + worldPosXToPixel(Poly[i].x) + " " + worldPosYToPixel(Poly[i].y));
            if (polyPixel.x < xmin)
              xmin = (int) polyPixel.x;
            if (polyPixel.x > xmax)
                xmax = (int) polyPixel.x;
            if (polyPixel.y < ymin)
                ymin = (int) polyPixel.y;
            if (polyPixel.y > ymax)
                ymax = (int) polyPixel.y;
        }
        
        Debug.Log("xmin: " + xmin + ", xmax: " + xmax + ", ymin: " + ymin + ", ymax: " + ymax);
        Vector2[] rectEG = new Vector2[2];
        rectEG[0] = new Vector2(xmin, ymin); // P1
        rectEG[1] = new Vector2(xmax, ymax); // P2
        return rectEG;
    }

    /*
     * Used by RemplissageRectEG to determine if a point is inside polygon
     */
    private bool interieur(int x, int y, Vector3[] poly)
    {
        // Only working for convex polygons
        for (int i = 0; i < poly.Length-1; i++)
        {
            if(!visible(new Vector3(x, y), new Vector3(worldPosXToPixel(poly[i].x), worldPosYToPixel(poly[i].y)), new Vector3(worldPosXToPixel(poly[i+1].x), worldPosYToPixel(poly[i+1].y)))) 
                return false;
        }
        
        return true;
    }

    /*
     * Change pixel color of sprite texture used for filling display
     */
    private void affichePixel(int x, int y)
    {
        img.sprite.texture.SetPixel(x, y, fillColor);
    }
    
    /*
     * Convert world position x axis value to pixel
     */
    private int worldPosXToPixel(float v)
    {
        return (int) (((v + 5.33) * Screen.width) / 10.66);
    }
    
    /*
     * Convert world position y axis value to pixel
     */
    private int worldPosYToPixel(float v)
    {
        return (int) (((v + 3) * Screen.height) / 6);
    }
    
    /*
     * Click actions event for drawing window
     */
    private void drawWindow()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPosition.z = 0f;
            lrWindow.positionCount = windowIndex + 1;
            lrWindow.SetPosition(windowIndex, mouseWorldPosition);
            windowIndex++;
        }

        if (Input.GetMouseButtonDown(1))
        {
            lrWindow.positionCount = windowIndex + 1;
            lrWindow.SetPosition(windowIndex, lrWindow.GetPosition(0));
            windowIndex++;
            drawingWindow = false;
            drawingPolygon = true;
            textWindow.SetActive(false);
            textPolygon.SetActive(true);
        }
    }

    /*
     * Click actions event for drawing polygon
     */
    private void drawPolygon()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPosition.z = 0f;
            lrPolygon.positionCount = polygonIndex + 1;
            lrPolygon.SetPosition(polygonIndex, mouseWorldPosition);
            polygonIndex++;
        }

        if (lrPolygon.positionCount > 1 && Input.GetMouseButtonDown(1))
        {
            lrPolygon.positionCount = polygonIndex + 1;
            lrPolygon.SetPosition(polygonIndex, lrPolygon.GetPosition(0));
            polygonIndex++;
            drawingPolygon = false;
            textPolygon.SetActive(false);
        }
    }
}
