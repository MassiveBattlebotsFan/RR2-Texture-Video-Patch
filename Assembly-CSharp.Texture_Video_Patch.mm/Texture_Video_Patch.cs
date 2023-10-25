using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using MonoMod;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using MonoMod.RuntimeDetour;


#pragma warning disable CS0626

class patch_Texture_Set : Texture_Set
{
    public static bool DEBUG = false;
    public patch_Texture_Set(patch_Texture_Set other) : base(other)
    {
        // this does nothing, gotta make the compiler happy
    }
    public extern void orig_ctor();
    public extern void orig_ctor(Texture2D main);
    public extern void orig_ctor(Texture_Set other);
        
    [MonoModConstructor]
    public void ctor()
    {
        this.secondaryPNGEncoded = new List<patch_Texture_Set.secondaryPNGDataThingy>();
        orig_ctor();
    }
    [MonoModConstructor]
    public void ctor(Texture2D main)
    {
        this.secondaryPNGEncoded = new List<patch_Texture_Set.secondaryPNGDataThingy>();
        orig_ctor(main);
    }
        
    [MonoModConstructor]
    public void ctor(patch_Texture_Set other){
        
        this.secondaryPNGEncoded = new List<patch_Texture_Set.secondaryPNGDataThingy>();

        if (other != null && other.secondaryPNGEncoded != null)
		{
			foreach (patch_Texture_Set.secondaryPNGDataThingy item in other.secondaryPNGEncoded)
			{
				if(DEBUG) Debug.Log("Added pngData");
				this.secondaryPNGEncoded.Add(item);
			}
        }
        orig_ctor(other);
    }

    public void recreateSecTextures(int index)
    {
        if (this.secondaryPNGEncoded == null || index > this.secondaryPNGEncoded.Count || this.secondaryPNGEncoded[index] == null || this.secondaryPNGEncoded[index].PNG == null)
        {
            if(DEBUG) Debug.Log("Invalid recreation");
            return;
        }
        this.mainTexture = new Texture2D(2, 2);
        this.mainTexture.LoadImage(this.secondaryPNGEncoded[index].PNG);
    }

    [Serializable]
    public class secondaryPNGDataThingy
    {
        // Token: 0x04000CC0 RID: 3264
        public byte[] PNG;
    }

    public List<patch_Texture_Set.secondaryPNGDataThingy> secondaryPNGEncoded;
}
class patch_Shape_Info_Simple : Shape_Info_Simple
{

    private void Start()
    {
        this.is_ready = true;
    }
    private void Update()
    {
        this.countdown--;
        if (this.texture_set != null && this.texture_index_max > 0 && this.countdown <= 0 && this.is_ready)
        {
            this.texture_set.recreateSecTextures(this.texture_index);
            if(patch_Texture_Set.DEBUG) Debug.Log("recreateSecTextures(" + this.texture_index.ToString() + ");");
            this.texture_index = (this.texture_index + 1) % this.texture_index_max;
            base.gameObject.GetComponent<Renderer>().material.mainTexture = this.texture_set.mainTexture;
            if(this.insideMeshObject != null) this.insideMeshObject.GetComponent<Renderer>().material.mainTexture = this.texture_set.mainTexture;
            this.countdown = 4;
        }
    }

    public extern void orig_setMaterial(Material material, Texture_Set textureSet);
    public void setMaterial(Material material, patch_Texture_Set textureSet)
    {
        if (textureSet != null) // added this bit
        {
            this.texture_set = new patch_Texture_Set(textureSet);
            if (patch_Texture_Set.DEBUG) Debug.Log("secondaryPNGEncoded size: " + this.texture_set.secondaryPNGEncoded.Count.ToString());
            this.texture_index_max = this.texture_set.secondaryPNGEncoded.Count;
        }
        orig_setMaterial(material, textureSet);
    }
    private int texture_index;

    private int texture_index_max;

    private patch_Texture_Set texture_set;

    private int countdown = 60;

    private bool is_ready;
}

