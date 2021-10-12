using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class ModelReader : MonoBehaviour
{
    static ModelReader reader;
    public static ModelReader Reader
    {
        get
        {
            return reader;
        }
        private set
        {
            if (reader == null)
                reader = value;
        }
    }

    private void Awake()
    {
        Reader = this;
    }

    bool LoadOff(string filePath, ref Model model)
    {
        try
        {
            string[] lines = System.IO.File.ReadAllLines(@filePath);
            int numOfVerticies, numOfTrianges, n;
            if (lines.Length >= 2)
            {
                if (lines[0] == null || lines[1] == null)
                    throw (new Exception("Lines are null"));

                if (!string.Equals(lines[0], ModelExtention.off.ToString(), StringComparison.CurrentCultureIgnoreCase))
                    throw (new Exception("First line is not off tag"));

                string[] values = lines[1].Split(' ');
                int.TryParse(values[0], out numOfVerticies);
                int.TryParse(values[1], out numOfTrianges);
                int.TryParse(values[2], out n);
            }
            else
                throw (new Exception("Number of line less than 2"));

            int lineIndex = 2;
            for (; lineIndex < numOfVerticies + 2; lineIndex++)
            {
                if (lines[lineIndex] == null) { model.ClearModel(); return false; }

                float x, y, z;
                string[] values = lines[lineIndex].Split(' ');
                float.TryParse(values[0], out x);
                float.TryParse(values[1], out y);
                float.TryParse(values[2], out z);
                model.AddVertex(x, y, z);

            }

            for (; lineIndex < numOfVerticies + numOfTrianges + 2; lineIndex++)
            {
                if (lines[lineIndex] == null) { model.ClearModel(); return false; }

                int p1, p2, p3;
                string[] values = lines[lineIndex].Split(' ');
                //values[0]???
                int.TryParse(values[1], out p1);
                int.TryParse(values[2], out p2);
                int.TryParse(values[3], out p3);

                model.AddTriangle(p1, p2, p3);
            }

        }
        catch (Exception e)
        {
            model.ClearModel();
            if (e != null)
                Debug.Log(e.ToString());
            else
                Debug.LogError("File cannot readed!");
            return false;
        }
        return true;
    }

    bool LoadObj(string filePath, ref Model model)
    {
        try
        {
            string[] lines = System.IO.File.ReadAllLines(@filePath);


            foreach (string line in lines)
            {
                string[] values = line.Split(' ');
                if (values.Length > 0)
                {
                    string identifyer = values[0];

                    switch (identifyer)
                    {
                        case "#":
                            if (!string.Equals(values[1], ModelExtention.obj.ToString(), StringComparison.CurrentCultureIgnoreCase))
                                throw (new Exception("File is not obj file"));
                            break;
                        case "v":
                            float x, y, z;
                            float.TryParse(values[1], out x);
                            float.TryParse(values[2], out y);
                            float.TryParse(values[3], out z);
                            model.AddVertex(x, y, z);
                            break;
                        case "f":
                            //int p1, p2, p3;

                            //int.TryParse(values[1], out p1);
                            //int.TryParse(values[2], out p2);
                            //int.TryParse(values[3], out p3);
                            //model.AddTriangle(p1, p2, p3);
                            break;
                    }
                }
                else
                {
                    throw (new Exception("Invalid Line"));
                }
            }


        }
        catch (Exception e)
        {
            model.ClearModel();
            if (e != null)
                Debug.Log(e.ToString());
            else
                Debug.LogError("File cannot readed!");
            return false;
        }

        return true;
    }
	/*
	string filePath; ModelExtention ext; Model model;

    public bool LoadModel(string filePath, ModelExtention ext, ref Model model)
    {
		if (threadIsRunning)
			return false;

		this.filePath = filePath;
		this.ext = ext;
		this.model = model;

		Thread thread = new Thread (LoadMeshTread);
		thread.Start ();

	return true;
	}
*/

	public bool LoadModel (string filePath, ModelExtention ext, ref Model model)
	{
		switch (ext) {
		case ModelExtention.off:
			return LoadOff (filePath, ref model);
		case ModelExtention.obj:
			return LoadObj (filePath, ref model);
		}

		return true;
	}
	/*
	bool threadIsRunning = false;
	void LoadMeshTread()
	{
		threadIsRunning = true;
		bool jobDone = false;
		while(!jobDone && threadIsRunning){
			ReadModel (filePath, ext, ref model);
		}
		threadIsRunning = false;
	}
*/
}
