using UnityEngine;
using SRNG;
using Delauney;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]

public class DunGenerator : MonoBehaviour
{
	public int minX = 3, minY = 3;
	public int  maxX = 11, maxY = 11;
	public int numChambers = 1;
	public int verticesCount = 4;
	public int tileSize = 1;
	public int sortPasses = 100;
	public string SeedNumber;
	Delaunay del;


	public Point2DList vertexList;
	
	public GameObject Room;
	ChamberList CList;
	ChamberList Bigrooms;
	public List<GameObject> Chams;




	// Use this for initialization
	void Start ()
	{

		vertexList = new Point2DList();
		BuildMesh ();

	}

	public void BuildMesh ()
	{
		SimpleRNG.SetSeedFromSystemTime ();
		BuildMesh (0);
	}

	public void BuildMesh (uint seed)
	{
		// seed up the random number generator
		if (seed != 0)
			SimpleRNG.SetSeed (seed);
		SeedNumber = SimpleRNG.m_w.ToString ();



		//initialize some variables for mesh generation
		Vector3[] vertices = new Vector3[0];
		Vector3[] normals = new Vector3[0];
		Vector2[] uv = new Vector2[0];
		//Triangle list
		int[] triangles = new int[0];


		// the list of Chamber data structures 
		CList = new ChamberList ();
		Chamber c;

		//generate a bunch of chambers with randomize data
		for (int i = 0; i < numChambers; i++) {
			
			c = new Chamber ();
			c.SetRange (maxX - minX, minX);
			c.SetData ();
			CList.Add (c);
		}

		//sort through the chambers to have them spaced out
		// after a number of passes remove remaining overlaps 
		for (int i = 0; i < sortPasses; i++) {
			SortChambers ();
			if (i == sortPasses - 1) {
				for (int j = CList.Count-1; j> 0; j--) {
					if (CList [j].Overlapping) {
						CList.RemoveAt (j);
					}
				}
			}
		}


		//generate Mesh data and create game objects that contain those meshes
		//will be moved to a single container
		for (int i = 0; i < CList.Count; i++) {
			
			c = CList [i];
			int numTiles = c.Width * c.Height;
			int numTris = numTiles * 2;
			
			int vSizeX = c.Width + 1;
			int vSizeZ = c.Height + 1;
			int numVerts = vSizeX * vSizeZ;
			
			vertices = new Vector3[numVerts];
			normals = new Vector3[numVerts];
			uv = new Vector2[numVerts];
			
			triangles = new int[numTris * 3]; 
			
			int x, z, vertCount;
			
			for (z = 0; z< vSizeZ; z++) {
				for (x = 0; x< vSizeX; x++) {
					
					vertCount = z * vSizeX + x;
					//we need length of tiles + 1
					vertices [vertCount].Set (x * tileSize - (vSizeX / 2), 0, -z * tileSize + (vSizeZ / 2));
					//all normals point up
					normals [vertCount].Set (0, 1, 0); 
					//seting UV Coord
					uv [vertCount] .Set ((float)x / c.Width, 1f - (float)z / c.Height);
					
				}
				
			}
			for (z = 0; z< c.Height; z++) {
				for (x = 0; x< c.Width; x++) {
					int squareIndex = z * c.Width + x;
					int triOffset = squareIndex * 6;
					triangles [triOffset + 0] = z * vSizeX + x + 0;
					triangles [triOffset + 1] = z * vSizeX + x + vSizeX + 1;
					triangles [triOffset + 2] = z * vSizeX + x + vSizeX + 0;
					
					triangles [triOffset + 3] = z * vSizeX + x + 0;
					triangles [triOffset + 4] = z * vSizeX + x + 1;
					triangles [triOffset + 5] = z * vSizeX + x + vSizeX + 1;
					
				}
			}
			
			
			GameObject cham = GameObject.Instantiate (Room, new Vector3 (c.CenterX, 0, c.CenterY), Quaternion.identity) as GameObject;
			//create Mesh and populate data;
			Mesh mesh = new Mesh ();
			
			mesh.vertices = vertices;
			mesh.triangles = triangles;
			mesh.normals = normals;
			mesh.uv = uv;
			MeshFilter meshFilter = cham.GetComponent<MeshFilter> ();
			MeshRenderer meshRender = cham.GetComponent<MeshRenderer> ();
			MeshCollider meshCollider = cham.GetComponent<MeshCollider> ();
			mesh.name = "Chamber " + i;
			cham.name = "Chamber " + i;
			if (c.Width > 6 && c.Height > 6) {
				
				
				meshRender.GetComponent<Renderer>().material.color = new Color (1, 0, 0, 1);
			}
			
			
			meshFilter.mesh = mesh;
			meshCollider.sharedMesh = mesh;
			cham.GetComponent<ChamberTest> ().Copy (c);
			Chams.Add (cham);
		}
		

		// find all the Larger rooms
		for (int i = 0; i < CList.Count; i++) {
			if (CList [i].Width > 6 && CList [i].Height > 6) {
				vertexList.Add(new Point2D(CList[i].CenterX,CList[i].CenterY));
			}
		}

		del = new Delaunay();
		del.eList = new EdgeList();
		del.elist2 = new EdgeList();
		vertexList.SortX();
		for (int i = 0; i < vertexList.Count; i++) {
			for (int j = i; j < vertexList.Count; j++) {
				del.eList.Add(new Edge(vertexList[i],vertexList[j]));
			}
		}
		del.Triangulate(vertexList);
		
	}

	void Update ()
	{
		/*if(del.triList.Count >0)
		foreach(Triangle tri in del.triList)
		{
			Vector3 pointA,pointB,pointC;
			pointA = new Vector3(tri.A.x,0,tri.A.y);
			pointB = new Vector3(tri.B.x,0,tri.B.y);
			pointC = new Vector3(tri.C.x,0,tri.C.y);
			Debug.DrawLine(pointA,pointB,Color.green,1f);
			Debug.DrawLine(pointB,pointC,Color.green,1f);
			Debug.DrawLine(pointC,pointA,Color.green,1f);

	
		}*/


		/*	foreach(Edge e in del.eList)
		{
			Debug.DrawLine(new Vector3(e.A.x,0,e.A.y),new Vector3(e.B.x,0,e.B.y));
		}*/
		foreach(Edge e in del.elist2)
		{
			Debug.DrawLine(new Vector3(e.A.x,0,e.A.y),new Vector3(e.B.x,0,e.B.y),Color.green);
		}


	}

	public void MakeDelaunay()
	{


		del.Triangulate(vertexList);


	}
	
	/*
	public void SortChambers ()
	{

		Chamber c = new Chamber ();
		for (int i = 0; i < numChambers; i++) {
				
			c = CList [i];
			foreach (Chamber other in CList) {
				if (c.CollidesWith (other)) {
					c.Neighbors.Add (other);
					c.Separate (other);
				}
			}

		}

		for (int i = 0; i < numChambers; i++) {

		}


	}

	*/

	/*
	public void SortChambers ()
	{
		
		Chamber c = new Chamber ();
		Chamber c2 = new Chamber ();

		for (int i = 0; i < numChambers; i++) {
			
			c = CList [i];
			c.Neighbors.Clear ();
			c.Overlapping = false;
			c.moveX = 0;
			c.moveY = 0;
			for (int j = 0; j < numChambers; j++) {
				c2 = CList [j];
				if (!(c.Equals (c2)) && c.CollidesWith (c2)) {
					c.Neighbors.Add (c2);
					c.Separate (c2);

				}
			}
		}
		for (int i = 0; i < numChambers; i++) {
			c = CList [i];
			if (c.moveX != 0) {
				c.Left += 1 * (int)Mathf.Sign (c.moveX);
				if (c.Width % 2 != 0)
					c.Left += 1 * (int)Mathf.Sign (c.moveX);
			}
			if (c.moveY != 0) {
				c.Top += 1 * (int)Mathf.Sign (c.moveY);
				if (c.Height % 2 != 0)
					c.Top += 1 * (int)Mathf.Sign (c.moveY);
			}
			Chams [i].transform.position = new Vector3 (c.CenterX, 0, c.CenterY);
			Chams [i].GetComponent<ChamberTest> ().Copy (c);
		}	
	}
	*/
	/*
	public void SortChambers ()
	{
		
		Chamber c = new Chamber ();
		Chamber c2 = new Chamber ();
		
		for (int i = 0; i < numChambers; i++) {
			
			c = CList [i];
			c.Neighbors.Clear ();
			c.Overlapping = false;
			c.moveX = 0;
			c.moveY = 0;
			for (int j = 0; j < numChambers; j++) {
				c2 = CList [j];
				if (!(c.Equals (c2)) && c.CollidesWith (c2)) {
					c.Neighbors.Add (c2);
					c.Separate (c2);
					
				}
			}
		}
		for (int i = 0; i < numChambers; i++) {
			c = CList [i];
			if (c.moveX != 0) {
				c.Left += 1 * (int)Mathf.Sign (c.moveX);
				if (c.Width % 2 != 0)
					c.Left += 1 * (int)Mathf.Sign (c.moveX);
			}
			if (c.moveY != 0) {
				c.Top += 1 * (int)Mathf.Sign (c.moveY);
				if (c.Height % 2 != 0)
					c.Top += 1 * (int)Mathf.Sign (c.moveY);
			}
		
		}	
	}
	 */
	public void SortChambers ()
	{
		
		Chamber c = new Chamber ();
		Chamber c2 = new Chamber ();
		
		for (int i = 0; i < numChambers; i++) {
			
			c = CList [i];
			c.Neighbors.Clear ();
			c.Overlapping = false;
			c.moveX = 0;
			c.moveY = 0;
			for (int j = 0; j < numChambers; j++) {
				c2 = CList [j];
				if (!(c.Equals (c2)) && c.CollidesWith (c2)) {
					c.Neighbors.Add (c2);
					c.Separate (c2);
					
				}
			}
		}

		for (int i = 0; i < numChambers; i++) {
			c = CList [i];
			c.Left += c.moveX;
			c.Top += c.moveY;
		
		}
	}
}

