using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.IO;

public class OZMLSave : MonoBehaviour
{
    public string LevelName = "dori0";
    public string DirectoryName = "mu_dori1";
    public bool DirectExport = true;
    public string DirectExportLocation = "";

    void SaveTextureToFile( Texture2D texture, string fileName )
    {
        string path = AssetDatabase.GetAssetPath( texture );

        if( path == "" ) return;

        TextureImporter ti = (TextureImporter)TextureImporter.GetAtPath( path );
        ti.isReadable = true;
        ti.textureFormat = TextureImporterFormat.RGBA32;
        AssetDatabase.ImportAsset( path );

        var bytes = texture.EncodeToPNG();
        BinaryWriter binary = new BinaryWriter( File.Open( fileName, FileMode.Create ) );
        binary.Write( bytes );
    }

    string GetMeshIdent( Mesh mesh )
    {
        return mesh.name + "_" + mesh.GetInstanceID();
    }

    void Start( )
    {
        string fileLocation = DirectExport ? DirectExportLocation : "";
        fileLocation += LevelName + ".xml";

        XmlWriter writer = XmlWriter.Create( fileLocation );
        writer.WriteStartDocument();
        writer.WriteStartElement( "ozml" );

        // Scene
        writer.WriteStartElement( "head" );
        writer.WriteStartElement( "scene" );

        /*string background = GetComponentInChildren<Camera>().backgroundColor.ToString();
        background = background.Substring( 5, background.Length - 13 );
        writer.WriteAttributeString( "background", background );

        string fog = RenderSettings.fogColor.ToString();
        fog = fog.Substring( 5, fog.Length - 6 );
        writer.WriteAttributeString( "fog", fog );

        writer.WriteEndElement();

        // Camera
        writer.WriteStartElement( "camera" );

        string camPositionString = transform.position.ToString();
        writer.WriteAttributeString( "position", camPositionString.Substring( 1, camPositionString.Length - 2 ) );

        writer.WriteEndElement();
        writer.Flush();

        // Music
        writer.WriteStartElement( "audio" );
        writer.WriteAttributeString( "url", "https://dl.dropbox.com/u/17070747/" + MusicName );
        writer.WriteEndElement();
        writer.Flush();*/

        // Materials
        writer.WriteStartElement( "materials" );

        Renderer[] exportObjects = gameObject.GetComponentsInChildren<Renderer>();
        Dictionary<string, Material> materialLibary = new Dictionary<string, Material>();

        foreach( Renderer renderer in exportObjects )
        {
            Material currentMaterial = renderer.sharedMaterial;
            if( !materialLibary.ContainsKey( currentMaterial.name ) )
            {
                SaveTextureToFile( currentMaterial.mainTexture as Texture2D, ( DirectExport ? DirectExportLocation : "" ) + "data/" + DirectoryName + "/" + currentMaterial.name + ".png" );
                materialLibary.Add( currentMaterial.name, currentMaterial );
            }
        }

        foreach( Material mat in materialLibary.Values.ToArray() )
        {
            writer.WriteStartElement( "material" );
            writer.WriteAttributeString( "name", mat.name );
            writer.WriteAttributeString( "texture", "data/" + DirectoryName + "/" + mat.name + ".png" );
            writer.WriteEndElement();
            writer.Flush();
        }

        writer.WriteEndElement();
        writer.Flush();

        // Meshes
        writer.WriteStartElement( "meshes" );

        Dictionary<int, Mesh> meshLibary = new Dictionary<int, Mesh>();

        foreach( Renderer renderer in exportObjects )
        {
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            if( filter == null )
                continue;

            Mesh currentMesh = filter.sharedMesh;
            if( !meshLibary.ContainsKey( currentMesh.GetInstanceID() ) )
            {
                ObjExporter.MeshToFile( filter, ( DirectExport ? DirectExportLocation : "" ) + "data/" + DirectoryName + "/", GetMeshIdent( currentMesh ) + ".obj" );
                meshLibary.Add( currentMesh.GetInstanceID(), currentMesh );
            }
        }

        foreach( Mesh mesh in meshLibary.Values.ToArray() )
        {
            writer.WriteStartElement( "mesh" );
            writer.WriteAttributeString( "name", GetMeshIdent( mesh ) );
            writer.WriteAttributeString( "url", "data/" + DirectoryName + "/" + GetMeshIdent( mesh ) + ".obj" );
            writer.WriteEndElement();
            writer.Flush();
        }

        writer.WriteEndElement();
        writer.Flush();

        // Geometry
        writer.WriteStartElement( "geometry" );

        int index = 0;
        foreach( Renderer obj in exportObjects )
        {
           MeshFilter filter = obj.GetComponent<MeshFilter>();
            if( filter == null )
                continue;

            Mesh currentMesh = filter.sharedMesh;
            string ident = GetMeshIdent( currentMesh );
            if( currentMesh.GetInstanceID() < 0 )
            {
                Debug.Log( ":<" );
                foreach( Mesh mesh in meshLibary.Values.ToArray() )
                    if( mesh.vertexCount == currentMesh.vertexCount )
                    {
                        ident = GetMeshIdent( mesh );
                        Debug.Log( ":>" );
                    }
            }

            writer.WriteStartElement( "meshInstance" );

            writer.WriteAttributeString( "name", obj.name + ":" + index );

            writer.WriteAttributeString( "mesh", ident );

            string matString = obj.GetComponent<Renderer>().material.name;
            writer.WriteAttributeString( "material", matString.Substring( 0, matString.Length - 11 ) );

            string posString = obj.transform.position.ToString( "F4" );
            writer.WriteAttributeString( "position", posString.Substring( 1, posString.Length - 2 ) );

            string scaString = obj.transform.lossyScale.ToString("F4");
            writer.WriteAttributeString( "scale", scaString.Substring( 1, scaString.Length - 2 ) );

            string rotString = obj.transform.eulerAngles.ToString( "F4" );
            writer.WriteAttributeString( "rotation", rotString.Substring( 1, rotString.Length - 2 ) );

            writer.WriteEndElement();
            writer.Flush();

            index++;
        }

        writer.WriteEndElement();

        writer.WriteEndElement();
        writer.WriteEndDocument();

        writer.Flush();
    }
}
