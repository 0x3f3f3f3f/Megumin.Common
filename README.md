# Common

com.megumin插件包公共代码。  
一些常用基础代码类型和特性。  

## Attribute
- ColorAttribute  
  用于定义UI显示颜色。  
- PathAttribute  
  用于String序列化成员，增加快速选中路径按钮。  
- ProtectedInInspectorAttribute  
  在检视面板中保护,只有先勾选才能修改。
- ReadOnlyInInspectorAttribute  
  在Inpector显示为只读。
- TagAttribute  
  用于string增加Tag下拉菜单。

## Class
- Cache  
  一种常用的多层缓存机制。  
- CodeGenerator  
  代码生成器，用于快速生成代码。  
- Enableable  
  增加一个Enabled成员，表示开启关闭。  
- GameObjectFilter  
  设置过滤GameObject。  
- GUIColorScope  
  GUI颜色作用域  
- MonoScriptExtension  
  仅编辑器，通过Type类型，找到定义Type的文件。  
- PathUtility  
  路径文件夹工具类。  
- ReEntryLock  
  重入锁。  
  保证一个长时间任务执行期间尝试多次调用，返回相同的任务，不多次开始新任务。任务完成后，则可以再次开启新任务。  
- TagMask  
  设置识别tag。  
- UnityMacro  
  字符串宏替换工具类。  
- UnityTraceListener  
  使用UnityDebug实现的TraceListener。  
- UnityWaitTime  
  对Time类进行抽象的计时工具类。  

