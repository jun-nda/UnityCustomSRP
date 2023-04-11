# UnityCustomSRP

learn unity custom srp


1.partial class

2. Canvas Mode
3. drawing legacy shaders
4. transparent render
5. culling,command。。。
6. renderqueue


 晕了半个下午终于清楚了。

（CPU）批处理：将使用同材质的Static物体，先转到世界空间坐标，然后把这些物体坐标都塞到一个VBO里。

draw的时候还是分别draw，但是因为材质球一样，所以不用切换状态，所以效率高了。

但缺点是，塞了一个新的大VBO，增加了内存。

（GPU）实例化：一次性将数据提交给GPU，然后GPU使用类似扫内存的方式，一次绘制完所有物体。

gpu显存上会存一个实例化数组，比如我存一个model矩阵数组或颜色数组，

然后gpu会根据instanceid去索引对应的数据。

至于为啥教程里没有实例化顶点数据，这个暂时不想去思考了。



Max Distance 会把圆形的剔除范围远端变成平的，有点意思，后面研究一下。
