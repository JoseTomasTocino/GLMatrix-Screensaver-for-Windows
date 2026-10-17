/* OpenGL/WGL port of XScreenSaver GLMatrix. MIT/X11; see LICENSE.txt. */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MatrixSCR
{
 internal sealed class MatrixRenderer : IDisposable
 {
  const int Grid=70, Wave=22, TW=512, TH=1024;
  const double Depth=35, Splash=.7;
  static readonly int[] Matrix={16,17,18,19,20,21,22,23,24,25,160,161,162,163,164,165,166,167,168,169,170,171,172,173,174,175};
  static readonly int[] Decimal={16,17,18,19,20,21,22,23,24,25};
  static readonly int[] Hex={16,17,18,19,20,21,22,23,24,25,33,34,35,36,37,38};
  static readonly int[] Binary={16,17}, Dna={33,35,39,52};
  static readonly double[,] Views={{0,0},{0,-20},{0,20},{25,0},{-25,0},{25,20},{-25,20},{25,-20},{-25,-20},{10,0},{-10,0},{0,0},{0,0},{0,0},{0,0},{0,0}};
  readonly SaverSettings cfg; readonly Random rng; readonly List<Strip> strips=new List<Strip>();
  readonly double[] ramp=new double[Wave]; readonly int[] alphabet; readonly bool mirror; readonly IntPtr hwnd;
  IntPtr dc,rc; uint texture; int width,height,count; float[] xyz,uv,rgba; GCHandle xyzPin,uvPin,rgbaPin;
  double vx,vy,fx,fy,tx,ty,elapsed,duration,pause; bool disposed,firstView=true,firstTiltPending=true;
  public string RendererName { get; private set; }

  public MatrixRenderer(IntPtr handle,int w,int h,SaverSettings settings,int seed)
  {
   hwnd=handle; cfg=settings; rng=new Random(seed); alphabet=Encoding(settings.Mode); mirror=settings.Mode=="Matrix";
   width=Math.Max(1,w); height=Math.Max(1,h); InitGl(); InitRamp();
   int n=Math.Max(1,Math.Min(2000,(int)(cfg.Density*2.2))), max=n*(Grid+1)*4;
   xyz=new float[max*3]; uv=new float[max*2]; rgba=new float[max*4];
   xyzPin=GCHandle.Alloc(xyz,GCHandleType.Pinned);uvPin=GCHandle.Alloc(uv,GCHandleType.Pinned);rgbaPin=GCHandle.Alloc(rgba,GCHandleType.Pinned);
   for(int i=0;i<n;i++){Strip s=new Strip();Reset(s);s.Yp=Next(Grid);s.Erase=rng.Next(3)==0;strips.Add(s);} Pick(true);
  }
  public void Resize(int w,int h){width=Math.Max(1,w);height=Math.Max(1,h);if(rc!=IntPtr.Zero){Current();GL.glViewport(0,0,width,height);Projection();}}
  public void Advance(double seconds){double f=seconds/.03;foreach(Strip s in strips)Tick(s,f);Track(seconds);Render();}
  public void Render()
  {
   if(disposed||rc==IntPtr.Zero)return; Current(); GL.glClear(GL.COLOR_BUFFER_BIT); GL.glMatrixMode(GL.MODELVIEW);GL.glLoadIdentity();
   GL.glTranslatef(0,0,-25);if(cfg.Rotate){GL.glRotatef((float)vx,1,0,0);GL.glRotatef((float)vy,0,1,0);} Build();Draw();Native.SwapBuffers(dc);
  }
  void InitGl()
  {
   dc=Native.GetDC(hwnd);if(dc==IntPtr.Zero)throw new InvalidOperationException("No se pudo obtener el contexto gráfico.");
   Native.PFD p=new Native.PFD();p.Size=(ushort)Marshal.SizeOf(typeof(Native.PFD));p.Version=1;p.Flags=0x25;p.PixelType=0;p.ColorBits=32;p.AlphaBits=8;p.LayerType=0;
   int pf=Native.ChoosePixelFormat(dc,ref p);if(pf==0||!Native.SetPixelFormat(dc,pf,ref p))throw new InvalidOperationException("No se pudo configurar OpenGL.");
   rc=Native.wglCreateContext(dc);if(rc==IntPtr.Zero)throw new InvalidOperationException("No se pudo crear el contexto OpenGL.");Current();RendererName=Marshal.PtrToStringAnsi(GL.glGetString(GL.RENDERER));
   GL.glClearColor(0,0,0,1);GL.glDisable(GL.DEPTH_TEST);GL.glDisable(GL.CULL_FACE);GL.glEnable(GL.TEXTURE_2D);GL.glEnable(GL.BLEND);GL.glBlendFunc(GL.SRC_ALPHA,GL.ONE);
   GL.glEnableClientState(GL.VERTEX_ARRAY);GL.glEnableClientState(GL.TEXCOORD_ARRAY);GL.glEnableClientState(GL.COLOR_ARRAY);GL.glViewport(0,0,width,height);Projection();Upload();Vsync();
  }
  void Projection(){double t=Math.Tan(40*Math.PI/180),r=t*width/Math.Max(1.0,height);GL.glMatrixMode(GL.PROJECTION);GL.glLoadIdentity();GL.glFrustum(-r,r,-t,t,1,100);GL.glMatrixMode(GL.MODELVIEW);}
  void Upload()
  {
   byte[] pix=TexturePixels();uint[] n={0};GL.glGenTextures(1,n);texture=n[0];GL.glBindTexture(GL.TEXTURE_2D,texture);GL.glPixelStorei(GL.UNPACK_ALIGNMENT,1);
   GL.glTexParameteri(GL.TEXTURE_2D,GL.MAG,GL.LINEAR);GL.glTexParameteri(GL.TEXTURE_2D,GL.MIN,GL.LINEAR);GL.glTexParameteri(GL.TEXTURE_2D,GL.WRAP_S,GL.REPEAT);GL.glTexParameteri(GL.TEXTURE_2D,GL.WRAP_T,GL.REPEAT);GL.glTexEnvi(GL.TEX_ENV,GL.TEX_MODE,GL.MODULATE);
   GCHandle pin=GCHandle.Alloc(pix,GCHandleType.Pinned);try{GL.glTexImage2D(GL.TEXTURE_2D,0,(int)GL.RGBA,TW,TH,0,GL.RGBA,GL.UBYTE,pin.AddrOfPinnedObject());}finally{pin.Free();}
  }
  byte[] TexturePixels()
  {
   byte[] dst=new byte[TW*TH*4];System.IO.Stream st=Assembly.GetExecutingAssembly().GetManifestResourceStream("MatrixSCR.Assets.matrix3.png");if(st==null)throw new InvalidOperationException("Falta matrix3.png.");
   using(st)using(Bitmap b=new Bitmap(st)){Rectangle rect=new Rectangle(0,0,b.Width,b.Height);BitmapData d=b.LockBits(rect,ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);try{
    int stride=Math.Abs(d.Stride);byte[] src=new byte[stride*b.Height];Marshal.Copy(d.Scan0,src,0,src.Length);
    for(int y=0;y<b.Height;y++){int sr=(d.Stride>0?y:b.Height-1-y)*stride,dr=y*TW*4;for(int x=0;x<b.Width;x++){int a=sr+x*4,o=dr+x*4;byte blue=src[a],green=src[a+1],red=src[a+2];dst[o]=red;dst[o+1]=255;dst[o+2]=blue;dst[o+3]=green;}}
   }finally{b.UnlockBits(d);}}return dst;
  }
  void Build()
  {
   count=0;strips.Sort(delegate(Strip a,Strip b){return a.Z.CompareTo(b.Z);});foreach(Strip s in strips){for(int i=0;i<Grid;i++){int g=s.G[i];bool on=s.Yp>=i;if(s.Erase)on=!on;if(g!=0&&on){double light=1;if(cfg.Waves){int j=Wave-((i+(Grid-s.WavePos))%Wave);if(j>=Wave)j=Wave-1;light=ramp[j];}Glyph(g,s.X,s.Y-i,s.Z,light);}}if(!s.Erase)Glyph(s.Feeder,s.X,s.Y-s.Yp,s.Z,1.5);}
  }
  void Glyph(int glyph,double x,double y,double z,double light)
  {
   if(cfg.Fog)light*=.2+(z/Depth+.5)*.8;if(z>Depth/2){double q=(z-Depth/2)/(Depth*Splash-Depth/2);light*=ramp[Math.Max(0,Math.Min(Wave-1,(int)(q*Wave)))];}
   int k=Math.Abs(glyph)-1;if(k>=160&&k<=175)k+=32;int col=k%16,row=k/16;float u0=col*32f/TW,u1=(col+1)*32f/TW,v0=row*46f/TH,v1=(row+1)*46f/TH;if(mirror){float q=u0;u0=u1;u1=q;}float a=(float)Math.Max(0,Math.Min(1.6,light));
   Vertex((float)x,(float)y,(float)z,u0,v1,a);Vertex((float)(x+1),(float)y,(float)z,u1,v1,a);Vertex((float)(x+1),(float)(y+1),(float)z,u1,v0,a);Vertex((float)x,(float)(y+1),(float)z,u0,v0,a);
  }
  void Vertex(float x,float y,float z,float u,float v,float a){int p=count*3;xyz[p]=x;xyz[p+1]=y;xyz[p+2]=z;p=count*2;uv[p]=u;uv[p+1]=v;p=count*4;rgba[p]=rgba[p+1]=rgba[p+2]=1;rgba[p+3]=a;count++;}
  void Draw()
  {
   if(count==0)return;GL.glBindTexture(GL.TEXTURE_2D,texture);GL.glVertexPointer(3,GL.FLOAT,0,xyzPin.AddrOfPinnedObject());GL.glTexCoordPointer(2,GL.FLOAT,0,uvPin.AddrOfPinnedObject());GL.glColorPointer(4,GL.FLOAT,0,rgbaPin.AddrOfPinnedObject());GL.glDrawArrays(GL.QUADS,0,count);
  }
  void Reset(Strip s)
  {
   s.X=Next(Grid)-Grid/2.0;s.Y=Grid/2.0+Bell(.5);s.Z=Depth*.2-Next(Depth*.7);s.Dz=Bell(.02)*cfg.Speed;s.Yp=0;s.Speed=Math.Max(.025,Bell(.3)*cfg.Speed);s.Erase=false;s.SpinEvery=Math.Max(1,Bell(2/cfg.Speed)+1);s.SpinTick=0;s.WavePos=0;s.WaveEvery=Math.Max(1,Bell(3/cfg.Speed)+1);s.WaveTick=0;
   for(int i=0;i<Grid;i++){bool draw=rng.Next(7)!=0,spin=draw&&rng.Next(20)==0;int g=draw?alphabet[rng.Next(alphabet.Length)]+1:0;s.G[i]=spin?-g:g;}s.Feeder=-(alphabet[rng.Next(alphabet.Length)]+1);
  }
  void Tick(Strip s,double f)
  {
   s.Z+=s.Dz*f;if(s.Z>Depth*Splash){Reset(s);return;}s.Yp+=s.Speed*f;if(s.Yp>=Grid){if(s.Erase){Reset(s);return;}s.Erase=true;s.Yp=0;s.Speed*=.5;}
   s.SpinTick+=f;if(s.SpinTick>s.SpinEvery){s.SpinTick=0;s.Feeder=-(alphabet[rng.Next(alphabet.Length)]+1);for(int i=0;i<Grid;i++)if(s.G[i]<0){s.G[i]=-(alphabet[rng.Next(alphabet.Length)]+1);if(rng.Next(800)==0)s.G[i]=-s.G[i];}}
   s.WaveTick+=f;if(s.WaveTick>s.WaveEvery){s.WaveTick=0;s.WavePos=(s.WavePos+1)%Wave;}
  }
  void InitRamp(){for(int i=0;i<Wave;i++){double v=(Wave-i)/(double)(Wave-1);ramp[i]=.2+Math.Sin(v*Math.PI/2)*.8;}}
  void Track(double sec){if(!cfg.Rotate)return;if(pause>0){pause-=sec;if(pause<=0)Pick(false);return;}elapsed+=sec;double t=Math.Min(1,elapsed/duration),e=Math.Sin(t*Math.PI/2);vx=fx+(tx-fx)*e;vy=fy+(ty-fy)*e;if(t>=1){vx=tx;vy=ty;if(firstView){firstView=false;pause=2;}else pause=4+Next(10);}}
  void Pick(bool first){fx=vx;fy=vy;int i;if(first)i=0;else if(firstTiltPending){i=3+rng.Next(8);firstTiltPending=false;}else i=rng.Next(Views.GetLength(0));tx=Views[i,0];ty=Views[i,1];elapsed=0;duration=first?.1:(8+Next(5))/cfg.Speed;pause=0;}
  void Vsync(){IntPtr p=Native.wglGetProcAddress("wglSwapIntervalEXT");long n=p.ToInt64();if(p==IntPtr.Zero||n<0&&n>=-3)return;try{((Native.SwapInterval)Marshal.GetDelegateForFunctionPointer(p,typeof(Native.SwapInterval)))(1);}catch{}}
  void Current(){if(!Native.wglMakeCurrent(dc,rc))throw new InvalidOperationException("No se pudo activar OpenGL.");}
  static int[] Encoding(string m){if(m=="Binary")return Binary;if(m=="Hexadecimal")return Hex;if(m=="Decimal")return Decimal;if(m=="DNA")return Dna;return Matrix;}
  double Next(double n){return rng.NextDouble()*n;}double Bell(double n){return(Next(n)+Next(n)+Next(n))/3;}
  public void Dispose(){if(disposed)return;disposed=true;if(rc!=IntPtr.Zero){Native.wglMakeCurrent(dc,rc);if(texture!=0)GL.glDeleteTextures(1,new uint[]{texture});Native.wglMakeCurrent(IntPtr.Zero,IntPtr.Zero);Native.wglDeleteContext(rc);rc=IntPtr.Zero;}if(dc!=IntPtr.Zero){Native.ReleaseDC(hwnd,dc);dc=IntPtr.Zero;}if(rgbaPin.IsAllocated)rgbaPin.Free();if(uvPin.IsAllocated)uvPin.Free();if(xyzPin.IsAllocated)xyzPin.Free();}
  sealed class Strip{public double X,Y,Z,Dz,Yp,Speed,SpinEvery,SpinTick,WaveEvery,WaveTick;public bool Erase;public int Feeder,WavePos;public readonly int[] G=new int[Grid];}

  static class Native
  {
   [StructLayout(LayoutKind.Sequential)]internal struct PFD{internal ushort Size,Version;internal uint Flags;internal byte PixelType,ColorBits,RedBits,RedShift,GreenBits,GreenShift,BlueBits,BlueShift,AlphaBits,AlphaShift,AccumBits,AccumRedBits,AccumGreenBits,AccumBlueBits,AccumAlphaBits,DepthBits,StencilBits,AuxBuffers;internal sbyte LayerType;internal byte Reserved;internal uint LayerMask,VisibleMask,DamageMask;}
   [UnmanagedFunctionPointer(CallingConvention.StdCall)]internal delegate bool SwapInterval(int n);
   [DllImport("user32.dll")]internal static extern IntPtr GetDC(IntPtr h);[DllImport("user32.dll")]internal static extern int ReleaseDC(IntPtr h,IntPtr d);
   [DllImport("gdi32.dll")]internal static extern int ChoosePixelFormat(IntPtr d,ref PFD p);[DllImport("gdi32.dll")]internal static extern bool SetPixelFormat(IntPtr d,int f,ref PFD p);[DllImport("gdi32.dll")]internal static extern bool SwapBuffers(IntPtr d);
   [DllImport("opengl32.dll")]internal static extern IntPtr wglCreateContext(IntPtr d);[DllImport("opengl32.dll")]internal static extern bool wglDeleteContext(IntPtr r);[DllImport("opengl32.dll")]internal static extern bool wglMakeCurrent(IntPtr d,IntPtr r);[DllImport("opengl32.dll",CharSet=CharSet.Ansi)]internal static extern IntPtr wglGetProcAddress(string n);
  }
  static class GL
  {
   internal const uint COLOR_BUFFER_BIT=0x4000,QUADS=7,FLOAT=0x1406,UBYTE=0x1401,RGBA=0x1908,RENDERER=0x1F01,MODELVIEW=0x1700,PROJECTION=0x1701,TEXTURE_2D=0x0DE1,BLEND=0x0BE2,DEPTH_TEST=0x0B71,CULL_FACE=0x0B44,SRC_ALPHA=0x0302,ONE=1,MAG=0x2800,MIN=0x2801,WRAP_S=0x2802,WRAP_T=0x2803,TEX_ENV=0x2300,TEX_MODE=0x2200,UNPACK_ALIGNMENT=0x0CF5,VERTEX_ARRAY=0x8074,COLOR_ARRAY=0x8076,TEXCOORD_ARRAY=0x8078;internal const int LINEAR=0x2601,REPEAT=0x2901,MODULATE=0x2100;
   [DllImport("opengl32.dll")]internal static extern IntPtr glGetString(uint n);[DllImport("opengl32.dll")]internal static extern void glClearColor(float a,float b,float c,float d);[DllImport("opengl32.dll")]internal static extern void glClear(uint m);[DllImport("opengl32.dll")]internal static extern void glViewport(int x,int y,int w,int h);[DllImport("opengl32.dll")]internal static extern void glMatrixMode(uint m);[DllImport("opengl32.dll")]internal static extern void glLoadIdentity();[DllImport("opengl32.dll")]internal static extern void glFrustum(double l,double r,double b,double t,double n,double f);[DllImport("opengl32.dll")]internal static extern void glTranslatef(float x,float y,float z);[DllImport("opengl32.dll")]internal static extern void glRotatef(float a,float x,float y,float z);[DllImport("opengl32.dll")]internal static extern void glEnable(uint x);[DllImport("opengl32.dll")]internal static extern void glDisable(uint x);[DllImport("opengl32.dll")]internal static extern void glBlendFunc(uint a,uint b);
   [DllImport("opengl32.dll")]internal static extern void glGenTextures(int n,[Out]uint[] t);[DllImport("opengl32.dll")]internal static extern void glDeleteTextures(int n,uint[] t);[DllImport("opengl32.dll")]internal static extern void glBindTexture(uint a,uint b);[DllImport("opengl32.dll")]internal static extern void glPixelStorei(uint a,int b);[DllImport("opengl32.dll")]internal static extern void glTexParameteri(uint a,uint b,int c);[DllImport("opengl32.dll")]internal static extern void glTexEnvi(uint a,uint b,int c);[DllImport("opengl32.dll")]internal static extern void glTexImage2D(uint a,int b,int c,int d,int e,int f,uint g,uint h,IntPtr p);
   [DllImport("opengl32.dll")]internal static extern void glEnableClientState(uint a);[DllImport("opengl32.dll")]internal static extern void glVertexPointer(int a,uint b,int c,IntPtr d);[DllImport("opengl32.dll")]internal static extern void glTexCoordPointer(int a,uint b,int c,IntPtr d);[DllImport("opengl32.dll")]internal static extern void glColorPointer(int a,uint b,int c,IntPtr d);[DllImport("opengl32.dll")]internal static extern void glDrawArrays(uint a,int b,int c);
  }
 }
}
