using UnityEngine;
using System.IO;
using System.Text;

public class ObjExporter
{

    public static string MeshToString( MeshFilter mf )
    {
        Mesh m = mf.mesh;

        // TODO unused
        //Material[] mats = mf.renderer.sharedMaterials;

        var sb = new StringBuilder();

        sb.Append( "g " ).Append( mf.name ).Append( "\n" );
        foreach( Vector3 v in m.vertices )
        {
            //Vector3 wv = mf.transform.TransformPoint( v );
            //sb.Append( string.Format( "v {0} {1} {2}\n", -wv.x, wv.y, wv.z ) );
            sb.Append( string.Format( "v {0} {1} {2}\n", v.x, v.y, v.z ) );
        }
        sb.Append( "\n" );
        foreach( Vector3 v in m.normals )
        {
            //Vector3 wv = mf.transform.TransformDirection( v );
           // sb.Append( string.Format( "vn {0} {1} {2}\n", -wv.x, wv.y, wv.z ) );
            sb.Append( string.Format( "vn {0} {1} {2}\n", v.x, v.y, v.z ) );
        }
        sb.Append( "\n" );
        foreach( Vector3 v in m.uv )
        {
            sb.Append( string.Format( "vt {0} {1}\n", v.x, v.y ) );
        }
       // for( int material = 0; material < m.subMeshCount; material++ )
        //{
            sb.Append( "\n" );
            //sb.Append( "usemtl " ).Append( mats[material].name ).Append( "\n" );
           // sb.Append( "usemap " ).Append( mats[material].name ).Append( "\n" );

           // print( m.subMeshCount )

            int[] triangles = m.GetTriangles( 0 );
            for( int i = 0; i < triangles.Length; i += 3 )
            {
                sb.Append( string.Format( "f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n",
                    triangles[i] + 1, triangles[i + 1] + 1, triangles[i + 2] + 1 ) );
            }
        //}
        return sb.ToString();
    }

    public static void MeshToFile( MeshFilter mf, string directory, string filename )
    {
        if( !Directory.Exists( directory ) )
        {
            Directory.CreateDirectory( directory );
        }

        using( StreamWriter sw = new StreamWriter( directory + filename ) )
        {
            sw.Write( MeshToString( mf ) );
        }
    }
}