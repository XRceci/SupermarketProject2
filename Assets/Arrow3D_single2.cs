using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Arrow3D_single2 : MonoBehaviour {


    private GameObject arrow3d;
    private Material mat;
    public GameObject arrow3d_single_prefab;
    private Vector3 locolPosition;
    private GameObject arrow3d_single_canvas;

    TaskManager manager;
    // Use this for initialization
    void Start () {
        arrow3d_single_canvas = this.gameObject;// GameObject.FindGameObjectWithTag("3DArrow_single_canvas");
        locolPosition = new Vector3(0f, -0.15f, 0.5f);

        arrow3d = Instantiate(arrow3d_single_prefab, arrow3d_single_canvas.transform);
        arrow3d.transform.localPosition = locolPosition;
        mat = arrow3d.GetComponentInChildren<MeshRenderer>().material;
    }
	
	// Update is called once per frame
	void Update () {
        
                arrow3d.SetActive(true);
                Vector3 lookAtPosition;
                lookAtPosition.x = manager.getCurrentSelectableObject().transform.position.x; //MainScript.manager.getClosestVector3(targets).x;
                lookAtPosition.y = manager.getCurrentSelectableObject().transform.position.x; //player.transform.position.y;
                lookAtPosition.z = manager.getCurrentSelectableObject().transform.position.z;
                arrow3d.transform.LookAt(lookAtPosition);

                //mat.color = ChangeTransparency(MainScript.manager.getClosestColor(targets));
        
	}

    private Color ChangeTransparency(Color32 color)
    {
        return new Color32(color.r, color.g, color.b, 117);
    }
}
