# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

<!--
## [Unreleased] - YYYY-MM-NN

### Added   
### Changed  
### Deprecated  
### Removed  
### Fixed  
### Security  
-->

---

## [Unreleased] - YYYY-MM-NN

## [1.0.7] - 2023-12-29
### Added   
- 增加选中堆栈
- 增加扩展函数测试GameObject父代子代
- 增加外部dll引用宏

### Changed  
- Enable默认为关闭状态

## [1.0.6] - 2023-10-29
### Added   
- 增加SerializedProperty扩展
- 增加GUI颜色作用域
- 增加扩展函数
 
### Fixed  
- 优化GameObjectFilter，尽量不访问GameObject.tag，会导致gc allocated

## [1.0.5] - 2023-10-08
### Added   
- 增加CodeGeneratorInfoAttribute  
- 抽象ICache接口  
- 增加新的同步Cache  
  
### Changed  
- 原Cache 重命名为 AsyncCache  

## [1.0.4] - 2023-09-29
### Changed 
- GameObjectFilter物理测试改为扩展函数实现。

## [1.0.3] - 2023-09-24
### Added  
- 增加多层缓存机制类型Cache。  
- 增加重入锁ReEntryLock。  
- 增加MonoScript查找缓存。  
- 增加WaitTime类。  
- 增加Path相关工具。  
- 增加快速复制文件工具。  

### Changed 
- Enableable 重命名为  Enable


## [1.0.2] - 2023-08-30
### Fixed  
- CodeGenerator 添加宏前判断宏是否存在

## [1.0.1] - 2023-08-28
### Added 
- CodeGenerator 生成的代码增加版本注释
- CodeGenerator 增加常用宏常量

### Fixed  
- CodeGenerator 修正生成标识符


## [0.0.1] - 2023-04-07
PackageWizard Fast Created.

