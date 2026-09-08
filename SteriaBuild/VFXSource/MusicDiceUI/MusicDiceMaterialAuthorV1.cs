using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class MusicGeneratedMaterialImport
{
    static int width,height;
    static Color[] raw;
    static bool[] background;
    static int[] distance;
    static readonly List<string> report=new List<string>();
    static string output,root,authorRoot;
    static Font font;
    static Transform canvas;
    static Camera camera;
    static RenderTexture render;

    public static void Run()
    {
        try
        {
            string[] args=Environment.GetCommandLineArgs();
            int authorIndex=Array.IndexOf(args,"-musicAuthorRoot"),outputIndex=Array.IndexOf(args,"-musicAuthorOutput");
            if(authorIndex<0||authorIndex+1>=args.Length||outputIndex<0||outputIndex+1>=args.Length)
                throw new ArgumentException("Use regenerate.ps1, or pass -musicAuthorRoot and -musicAuthorOutput explicitly.");
            authorRoot=Path.GetFullPath(args[authorIndex+1]);
            root=Path.GetFullPath(Path.Combine(authorRoot,"../../.."));
            output=Path.GetFullPath(args[outputIndex+1]);
            Directory.CreateDirectory(output);
            string sourcePath=Path.Combine(authorRoot,"Authoring/v1/blank-rgb.png");
            if(Hash(sourcePath)!="2BD2C3085DE04C5BA1F7C55FB05EFB921980C29059D949526F1965E4450A9FA9")throw new Exception("Reviewed component correction requires the locked original v1 source.");
            var source=Load(sourcePath);width=source.width;height=source.height;raw=source.GetPixels();
            report.Add("Source: "+sourcePath);report.Add("Source SHA256: "+Hash(sourcePath));
            report.Add("Selected v1: first style-directed output; second output did not fix transparency and added heavier exterior marks.");
            report.Add("Source is RGB with a painted checkerboard. Alpha below is deterministic C# texture-import processing, NOT native image_gen alpha.");
            FindExterior();
            ResolveEnclosedChecker();
            var pixels=MatteEdges();
            var full=Texture(width,height,pixels);
            File.WriteAllBytes(Path.Combine(output,"blank-alpha-full.png"),full.EncodeToPNG());
            Rect body=BrightBody(pixels);
            float scale=Mathf.Min(156f/body.width,170f/body.height);
            var blank=Fit(pixels,body.center,scale);
            var glyph=Load(Path.Combine(authorRoot,"Authoring/v1/Glyph.png"));
            var card=Texture(256,256,blank.GetPixels());
            Composite(card,glyph,51.2f,51.2f,153.6f,153.6f);
            File.WriteAllBytes(Path.Combine(output,"BlankFrame.png"),blank.EncodeToPNG());
            File.WriteAllBytes(Path.Combine(output,"Card.png"),card.EncodeToPNG());
            File.Copy(Path.Combine(authorRoot,"Authoring/v1/Glyph.png"),Path.Combine(output,"Glyph.png"),true);
            report.Add("Visible blue main-body source bbox: "+body+"; fit scale: "+scale.ToString("F6")+"; target main body: "+(body.size*scale));
            report.Add("Fit is about the measured bright blue painted frame, not the RGB canvas/checkerboard or isolated black ink specks.");
            report.Add("Glyph: unchanged own 80% RGBA at original card-local placement (51.2,51.2,153.6,153.6); no runtime/UI/source asset change.");
            CheckAlpha(blank,"BlankFrame");CheckAlpha(card,"Card");
            Contact(blank,card);
            ComponentEvidence(pixels);
            foreach(string name in new[]{"BlankFrame.png","Card.png","Glyph.png","blank-alpha-full.png","contact.png","enclosed-components-contact.png","enclosed-components.csv"})
                report.Add(name+" SHA256: "+Hash(Path.Combine(output,name)));
            File.WriteAllLines(Path.Combine(output,"processing-report.txt"),report.ToArray());
            Debug.Log("MUSIC_GENERATED_IMPORT_READY "+output);
            EditorApplication.Exit(0);
        }
        catch(Exception ex){report.Add("FAILED: "+ex);if(output!=null)File.WriteAllLines(Path.Combine(output,"processing-report.txt"),report.ToArray());Debug.LogException(ex);EditorApplication.Exit(1);}
    }
    static Texture2D Load(string path){var t=new Texture2D(2,2,TextureFormat.RGBA32,false);if(!t.LoadImage(File.ReadAllBytes(path)))throw new Exception("Cannot decode "+path);return t;}
    static string Hash(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","");}
    static Texture2D Texture(int w,int h,Color[] p){var t=new Texture2D(w,h,TextureFormat.RGBA32,false);t.filterMode=FilterMode.Bilinear;t.wrapMode=TextureWrapMode.Clamp;t.SetPixels(p);t.Apply();return t;}
    static bool Key(Color c){float low=Mathf.Min(c.r,Mathf.Min(c.g,c.b)),high=Mathf.Max(c.r,Mathf.Max(c.g,c.b));return low>.43f&&high-low<.10f;}
    static void FindExterior()
    {
        int n=raw.Length;background=new bool[n];distance=new int[n];for(int i=0;i<n;i++)distance[i]=-1;
        var queue=new Queue<int>();
        for(int x=0;x<width;x++){Seed(x,queue);Seed((height-1)*width+x,queue);}for(int y=0;y<height;y++){Seed(y*width,queue);Seed(y*width+width-1,queue);}
        while(queue.Count>0){int i=queue.Dequeue(),x=i%width,y=i/width;for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){int xx=x+dx,yy=y+dy;if(xx<0||yy<0||xx>=width||yy>=height)continue;int j=yy*width+xx;if(!background[j]&&Key(raw[j])){background[j]=true;queue.Enqueue(j);}}}
        int removed=0;for(int i=0;i<n;i++)if(background[i]){removed++;distance[i]=0;queue.Enqueue(i);}
        while(queue.Count>0){int i=queue.Dequeue();if(distance[i]>=12)continue;int x=i%width,y=i/width;for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){int xx=x+dx,yy=y+dy;if(xx<0||yy<0||xx>=width||yy>=height)continue;int j=yy*width+xx;if(distance[j]<0){distance[j]=distance[i]+1;queue.Enqueue(j);}}}
        report.Add("Exterior-connected neutral checker pixels removed: "+removed+" / "+n+". Key: minRGB>.43 and maxRGB-minRGB<.10; 8-connected to image boundary.");
    }
    sealed class EnclosedPatch
    {
        public int Id,Area;
        public Rect Roi;
        public float Neutral,Blue,Palette,Spread,Fill,MeanBlue;
        public bool Removed;
        public string Reason;
    }
    static readonly List<EnclosedPatch> patches=new List<EnclosedPatch>();
    static void ResolveEnclosedChecker()
    {
        int[] palette=new int[256];int exteriorCount=0;
        for(int i=0;i<raw.Length;i++)if(background[i]){palette[Mathf.Clamp(Mathf.RoundToInt((raw[i].r+raw[i].g+raw[i].b)*85f),0,255)]++;exteriorCount++;}
        Rect blueBody=BrightBody(raw);bool[] seen=new bool[raw.Length];int removedPixels=0,keptPixels=0;
        for(int seed=0;seed<raw.Length;seed++)
        {
            if(background[seed]||seen[seed]||!Key(raw[seed]))continue;
            var component=new List<int>{seed};seen[seed]=true;
            for(int n=0;n<component.Count;n++){int x=component[n]%width,y=component[n]/width;for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){int xx=x+dx,yy=y+dy;if(xx<0||yy<0||xx>=width||yy>=height)continue;int j=yy*width+xx;if(!background[j]&&!seen[j]&&Key(raw[j])){seen[j]=true;component.Add(j);}}}
            int x0=width,y0=height,x1=0,y1=0,neutral=0,cool=0,tone=0,near=0;float low=1,high=0,bias=0;
            foreach(int i in component)
            {
                int x=i%width,y=i/width;x0=Mathf.Min(x0,x);x1=Mathf.Max(x1,x);y0=Mathf.Min(y0,y);y1=Mathf.Max(y1,y);
                Color c=raw[i];float l=(c.r+c.g+c.b)/3f,spread=Mathf.Max(c.r,Mathf.Max(c.g,c.b))-Mathf.Min(c.r,Mathf.Min(c.g,c.b));
                if(spread<.045f&&Mathf.Abs(c.b-c.r)<.025f)neutral++;
                if(c.b-c.r>.025f)cool++;
                int level=Mathf.Clamp(Mathf.RoundToInt(l*255),0,255);int local=palette[level]+(level>0?palette[level-1]:0)+(level<255?palette[level+1]:0);
                if(l>.55f&&local>exteriorCount*.00015f)tone++;
                if(distance[i]>0&&distance[i]<=12)near++;
                low=Mathf.Min(low,l);high=Mathf.Max(high,l);bias+=c.b-c.r;
            }
            var roi=new Rect(x0,y0,x1-x0+1,y1-y0+1);float count=component.Count;
            float neutralRatio=neutral/count,paletteRatio=tone/count,blueBias=bias/count,fill=count/(roi.width*roi.height);
            var relative=roi.center-blueBody.center;
            float radial=Mathf.Sqrt(Mathf.Pow(relative.x/(blueBody.width*.5f),2)+Mathf.Pow(relative.y/(blueBody.height*.5f),2));
            bool exteriorSide=near>0||radial>.92f||!blueBody.Contains(roi.center);
            bool block=fill>.40f&&roi.width>=3&&roi.height>=3;
            bool repeatedTones=high-low>.065f;
            bool neutralChecker=neutralRatio>.70f&&blueBias<.035f&&(block||repeatedTones||(count<=9&&near>0));
            bool blueWashedChecker=blueBias<.060f&&repeatedTones&&fill>.35f&&radial>.92f;
            // Main-thread visual review confirmed this exact original-source component as a blue-washed 2x2 checker tile.
            // Only its neutral-key membership is removed; the other colored brush pixels inside the ROI are untouched.
            bool reviewedC186=component.Count==105&&x0==214&&y0==944&&roi.width==11&&roi.height==11;
            bool remove=reviewedC186||(exteriorSide&&paletteRatio>.75f&&(neutralChecker||blueWashedChecker));
            if(reviewedC186){int colored=0;for(int yy=y0;yy<=y1;yy++)for(int xx=x0;xx<=x1;xx++)if(!Key(raw[yy*width+xx])&&!background[yy*width+xx])colored++;report.Add("Reviewed C186: remove 105 exact checker pixels at [214,944,11,11]; retain "+colored+" non-key colored brush pixels within that ROI. Blue bias does not override the visible axis-aligned checker evidence.");}
            string reason=reviewedC186?"reviewed source-locked blue-washed 2x2 checker; exact membership only":remove?(neutralChecker?"neutral checker palette + block/tone structure outside paint core":"blue-washed checker: alternating neutral tile tones outside main blue contour")
                :blueBias>=.035f?"preserve blue-white brush: sustained blue bias, not a neutral tile"
                :"preserve paint fleck: no repeated checker/block structure or no exterior palette match";
            var patch=new EnclosedPatch{Id=patches.Count+1,Area=component.Count,Roi=roi,Neutral=neutralRatio,Blue=cool/count,Palette=paletteRatio,Spread=high-low,Fill=fill,MeanBlue=blueBias,Removed=remove,Reason=reason};patches.Add(patch);
            if(remove){foreach(int i in component)background[i]=true;removedPixels+=component.Count;}else keptPixels+=component.Count;
        }
        var csv=new StringBuilder("id,area,x_bottom_origin,y_bottom_origin,width,height,neutral_fraction,blue_fraction,background_palette_fraction,luma_span,box_fill,mean_blue_bias,decision,reason\n");
        foreach(var p in patches)csv.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,"{0},{1},{2},{3},{4},{5},{6:F4},{7:F4},{8:F4},{9:F4},{10:F4},{11:F4},{12},{13}\n",p.Id,p.Area,p.Roi.x,p.Roi.y,p.Roi.width,p.Roi.height,p.Neutral,p.Blue,p.Palette,p.Spread,p.Fill,p.MeanBlue,p.Removed?"REMOVE_CHECKER":"KEEP_PAINT",p.Reason);
        File.WriteAllText(Path.Combine(output,"enclosed-components.csv"),csv.ToString());
        report.Add("Enclosed component classification: "+patches.Count+" patches; removed verified checker pixels="+removedPixels+"; retained paint pixels="+keptPixels+". Full area/ROI/color/structure evidence: enclosed-components.csv.");
        RebuildDistances();
    }
    static void RebuildDistances()
    {
        var q=new Queue<int>();for(int i=0;i<distance.Length;i++){distance[i]=background[i]?0:-1;if(background[i])q.Enqueue(i);}
        while(q.Count>0){int i=q.Dequeue();if(distance[i]>=12)continue;int x=i%width,y=i/width;for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){int xx=x+dx,yy=y+dy;if(xx<0||yy<0||xx>=width||yy>=height)continue;int j=yy*width+xx;if(distance[j]<0){distance[j]=distance[i]+1;q.Enqueue(j);}}}
    }
    static Texture2D Crop(Color[] source,Rect roi){int x0=Mathf.Max(0,(int)roi.x-14),y0=Mathf.Max(0,(int)roi.y-14),w=Mathf.Min(width-x0,(int)roi.width+28),h=Mathf.Min(height-y0,(int)roi.height+28);var p=new Color[w*h];for(int y=0;y<h;y++)for(int x=0;x<w;x++)p[y*w+x]=source[(y0+y)*width+x0+x];return Texture(w,h,p);}
    static void ComponentEvidence(Color[] clean)
    {
        if(canvas!=null)UnityEngine.Object.DestroyImmediate(canvas.gameObject);if(camera!=null)UnityEngine.Object.DestroyImmediate(camera.gameObject);if(render!=null)UnityEngine.Object.DestroyImmediate(render);
        var chosen=new List<EnclosedPatch>(patches);chosen.Sort((a,b)=>b.Area.CompareTo(a.Area));if(chosen.Count>12)chosen.RemoveRange(12,chosen.Count-12);
        const int w=1200;int h=80+((chosen.Count+1)/2)*230;
        camera=new GameObject("ComponentEvidenceCamera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.09f,.09f,.09f);camera.orthographic=true;camera.orthographicSize=h/2;camera.transform.position=new Vector3(0,0,-10);render=new RenderTexture(w,h,24);camera.targetTexture=render;
        var g=new GameObject("ComponentEvidenceCanvas",typeof(Canvas));canvas=g.transform;var cv=g.GetComponent<Canvas>();cv.renderMode=RenderMode.ScreenSpaceCamera;cv.worldCamera=camera;cv.planeDistance=10;
        Label("Largest enclosed regions: original RGB / cleaned on black / cleaned on gray",15,15,1170,24,Color.white);
        for(int i=0;i<chosen.Count;i++)
        {
            var p=chosen[i];float x=(i%2)*600,y=70+(i/2)*230;
            Label("C"+p.Id+" "+(p.Removed?"CHECKER REMOVED":"BLUE/PAINT KEPT")+" area="+p.Area,x+10,y,580,18,Color.white);
            Label("ROI "+p.Roi+"; blue="+p.MeanBlue.ToString("F3")+", tone span="+p.Spread.ToString("F3"),x+10,y+28,580,15,new Color(.75f,.8f,.85f));
            var before=Crop(raw,p.Roi);var after=Crop(clean,p.Roi);Img(before,x+12,y+65,174,150);
            var black=Rect("black evidence",x+202,y+65,174,150).gameObject.AddComponent<Image>();black.color=Color.black;Img(after,x+202,y+65,174,150);
            var gray=Rect("gray evidence",x+392,y+65,174,150).gameObject.AddComponent<Image>();gray.color=new Color(.76f,.76f,.76f);Img(after,x+392,y+65,174,150);
        }
        Canvas.ForceUpdateCanvases();camera.Render();var previous=RenderTexture.active;RenderTexture.active=render;var image=new Texture2D(w,h,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,w,h),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,"enclosed-components-contact.png"),image.EncodeToPNG());RenderTexture.active=previous;
    }
    static void Seed(int i,Queue<int> q){if(!background[i]&&Key(raw[i])){background[i]=true;q.Enqueue(i);}}
    static Color[] MatteEdges()
    {
        var result=new Color[raw.Length];int fractional=0,deepNeutral=0;int x0=width,y0=height,x1=0,y1=0;
        for(int i=0;i<raw.Length;i++)
        {
            if(background[i]){result[i]=Color.clear;continue;}
            Color c=raw[i];c.a=1;int x=i%width,y=i/width;
            if(Key(c)&&(distance[i]<0||distance[i]>8)){deepNeutral++;x0=Mathf.Min(x0,x);y0=Mathf.Min(y0,y);x1=Mathf.Max(x1,x);y1=Mathf.Max(y1,y);}
            if(distance[i]>0&&distance[i]<=8)
            {
                int bestB=int.MaxValue,bestF=int.MaxValue;Color bg=Color.white,fg=c;
                for(int dy=-12;dy<=12;dy++)for(int dx=-12;dx<=12;dx++)
                {
                    int xx=x+dx,yy=y+dy;if(xx<0||yy<0||xx>=width||yy>=height)continue;int j=yy*width+xx,d=dx*dx+dy*dy;
                    if(background[j]&&d<bestB){bestB=d;bg=raw[j];}
                    else if(!background[j]&&(distance[j]<0||distance[j]>=4)&&!Key(raw[j])&&d<bestF){bestF=d;fg=raw[j];}
                }
                if(bestB<int.MaxValue&&bestF<int.MaxValue)
                {
                    Vector3 f=new Vector3(fg.r-bg.r,fg.g-bg.g,fg.b-bg.b),v=new Vector3(c.r-bg.r,c.g-bg.g,c.b-bg.b);
                    float alpha=f.sqrMagnitude>.015f?Mathf.Clamp01(Vector3.Dot(v,f)/f.sqrMagnitude):1f;
                    if(alpha>.025f){c.r=Mathf.Clamp01((c.r-bg.r*(1-alpha))/alpha);c.g=Mathf.Clamp01((c.g-bg.g*(1-alpha))/alpha);c.b=Mathf.Clamp01((c.b-bg.b*(1-alpha))/alpha);c.a=alpha;}
                    else c=Color.clear;
                    if(c.a>0&&c.a<.99f)fractional++;
                }
            }
            result[i]=c;
        }
        report.Add("Locally decontaminated fractional edge pixels: "+fractional+". Matte estimated from nearby exterior checker and inward confident paint; no geometric hex crop.");
        report.Add("Retained paint-colored neutral-key pixels deeper than 8px (classification evidence recorded): "+deepNeutral+"; bottom-origin ROI ["+x0+","+y0+","+x1+","+y1+"]. See component decisions and local before/after contact; no unclassified checker placeholder.");
        return result;
    }
    static Rect BrightBody(Color[] p)
    {
        bool[] seen=new bool[p.Length];var largest=new List<int>();
        for(int i=0;i<p.Length;i++)
        {
            if(seen[i]||!Bright(p[i]))continue;var component=new List<int>{i};seen[i]=true;
            for(int k=0;k<component.Count;k++){int x=component[k]%width,y=component[k]/width;for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){int xx=x+dx,yy=y+dy;if(xx<0||yy<0||xx>=width||yy>=height)continue;int j=yy*width+xx;if(!seen[j]&&Bright(p[j])){seen[j]=true;component.Add(j);}}}
            if(component.Count>largest.Count)largest=component;
        }
        if(largest.Count<500)throw new Exception("Cannot isolate the painted main frame for optical sizing.");
        int a=width,b=height,c=0,d=0;foreach(int i in largest){a=Mathf.Min(a,i%width);b=Mathf.Min(b,i/width);c=Mathf.Max(c,i%width);d=Mathf.Max(d,i/width);}var rect=new Rect(a,b,c-a+1,d-b+1);
        if(rect.width<500||rect.height<600)throw new Exception("Main-frame support fragmented; inspect sizing ROI "+rect);
        return rect;
    }
    static bool Bright(Color c){return c.b-c.r>.10f&&c.b*c.a>.42f;}
    static Color Premul(Color c){return new Color(c.r*c.a,c.g*c.a,c.b*c.a,c.a);}
    static Color Unpremul(Color c){if(c.a>.00001f){c.r/=c.a;c.g/=c.a;c.b/=c.a;}return c;}
    static Color Sample(Color[] p,int w,int h,float x,float y){int ix=Mathf.FloorToInt(x),iy=Mathf.FloorToInt(y);return Color.Lerp(Color.Lerp(Pixel(p,w,h,ix,iy),Pixel(p,w,h,ix+1,iy),x-ix),Color.Lerp(Pixel(p,w,h,ix,iy+1),Pixel(p,w,h,ix+1,iy+1),x-ix),y-iy);}
    static Color Pixel(Color[] p,int w,int h,int x,int y){return x<0||y<0||x>=w||y>=h?Color.clear:Premul(p[y*w+x]);}
    static Texture2D Fit(Color[] p,Vector2 center,float scale){var outputPixels=new Color[256*256];for(int y=0;y<256;y++)for(int x=0;x<256;x++){Color sum=Color.clear;for(int sy=0;sy<4;sy++)for(int sx=0;sx<4;sx++)sum+=Sample(p,width,height,(x+(sx+.5f)/4-128)/scale+center.x-.5f,(y+(sy+.5f)/4-128)/scale+center.y-.5f);outputPixels[y*256+x]=Unpremul(sum/16f);}return Texture(256,256,outputPixels);}
    static void Composite(Texture2D target,Texture2D insert,float left,float bottom,float w,float h){var p=target.GetPixels();var src=insert.GetPixels();for(int y=Mathf.Max(0,Mathf.FloorToInt(bottom));y<Mathf.Min(target.height,bottom+h);y++)for(int x=Mathf.Max(0,Mathf.FloorToInt(left));x<Mathf.Min(target.width,left+w);x++){Color front=Sample(src,insert.width,insert.height,(x+.5f-left)*insert.width/w-.5f,(y+.5f-bottom)*insert.height/h-.5f);int i=y*target.width+x;Color back=Premul(p[i]);p[i]=Unpremul(front+back*(1-front.a));}target.SetPixels(p);target.Apply();}
    static void CheckAlpha(Texture2D t,string name){int zero=0,partial=0,full=0;foreach(var c in t.GetPixels()){if(c.a<.001f)zero++;else if(c.a>.999f)full++;else partial++;}if(zero==0||partial==0||full==0)throw new Exception(name+" lacks a usable RGBA alpha distribution");report.Add(name+" RGBA alpha: zero="+zero+", fractional="+partial+", opaque="+full);}
    static RectTransform Rect(string name,float x,float y,float w,float h){var g=new GameObject(name,typeof(RectTransform));var r=g.GetComponent<RectTransform>();r.SetParent(canvas,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
    static void Img(Texture2D texture,float x,float y,float w,float h){var image=Rect("image",x,y,w,h).gameObject.AddComponent<Image>();image.sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f));image.preserveAspect=true;}
    static void Label(string value,float x,float y,float w,int size,Color color){var t=Rect("label",x,y,w,50).gameObject.AddComponent<Text>();t.font=font;t.text=value;t.fontSize=size;t.color=color;}
    static void Contact(Texture2D blank,Texture2D card)
    {
        const int w=1080,h=950;camera=new GameObject("CandidateCamera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.08f,.08f);camera.orthographic=true;camera.orthographicSize=h/2;camera.transform.position=new Vector3(0,0,-10);render=new RenderTexture(w,h,24);camera.targetTexture=render;var g=new GameObject("CandidateCanvas",typeof(Canvas));canvas=g.transform;var cv=g.GetComponent<Canvas>();cv.renderMode=RenderMode.ScreenSpaceCamera;cv.worldCamera=camera;cv.planeDistance=10;font=Font.CreateDynamicFontFromOSFont("Arial",22);
        Color[] backgrounds={Color.black,new Color(.10f,.065f,.045f),new Color(.76f,.76f,.76f)};string[] labels={"BLACK","DARK BROWN","LIGHT GRAY"};
        for(int i=0;i<3;i++){var panel=Rect("background",i*360,0,360,720).gameObject.AddComponent<Image>();panel.color=backgrounds[i];Color text=i==2?Color.black:Color.white;Label(labels[i],i*360+22,15,320,23,text);Label("RGBA blank / 256px slot",i*360+22,53,325,19,text);Img(blank,i*360+52,95,256,256);Label("Same own glyph / 256px slot",i*360+22,390,325,19,text);Img(card,i*360+52,432,256,256);}
        Label("Same 32px slots: native Slash / Pierce / Hit / Counter / generated blank / music",20,750,1040,22,Color.white);
        string[] names={"CardAttack_0_AfterIcon_0_8","CardAttack_1_AfterIcon_0_9","CardAttack_2_AfterIcon_0_10","CardStandby_0_AfterIcon_3_10"};for(int i=0;i<4;i++)Img(Load(Path.Combine(authorRoot,"Authoring/v1/StyleReferences/"+names[i]+".png")),55+i*165,820,32,32);Img(blank,715,820,32,32);Img(card,880,820,32,32);Label("Engineered alpha from RGB image_gen output; no production assets changed",20,900,1040,21,Color.white);
        Canvas.ForceUpdateCanvases();camera.Render();var previous=RenderTexture.active;RenderTexture.active=render;var image=new Texture2D(w,h,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,w,h),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,"contact.png"),image.EncodeToPNG());RenderTexture.active=previous;
    }
}
