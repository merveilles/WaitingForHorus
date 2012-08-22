Shader "No Fog" {

   SubShader {

    Fog {Mode Linear}

    Fog {Range 50, 120}

    

      BindChannels {

         Bind "Color", color

         Bind "Vertex", vertex

         Bind "TexCoord", texcoord

         

         

      }

      Pass {

      }

   }

}