# NTQQ Sign Server - 基于ASP.NET Core 签名服务器

这是一个基于 ASP.NET Core 重写的 NTQQ 签名服务器(虽然还是用了symbols.c去生成libsymbols.so)

首先肯定是生成可执行文件(不多bb)
### 1. 配置签名服务
第一步，把Linux NTQQ安装包里的/opt/QQ/resources/app文件夹里面的所有复制到项目QQApp文件夹下
第二步 执行
```bash
gcc -std=c99 -shared -fPIC -o libsymbols.so symbols.c
```
第三步编辑 `appsettings.json` 文件，配置签名服务参数：
```bash
{
  "AppSettings": {
    "SignService": {
      "Host": "127.0.0.1",//服务器监听地址
      "Port": 8080,//监听端口
      "Libs": ["libgnutls.so.30", "./libsymbols.so"],
      "Offset": "0x0",//Sign函数偏移，自行查找
      "MaxDataLength": 1048576,//最大数据长度，默认1MB，一般无需修改
      "TimeoutMs": 5000//超时时间，默认5000ms，一般无需修改
    }
  }
}
```bash
第四步编辑appinfo.json文件，配置版本
如果你连如何获取版本都不会，那为啥要用这个项目？
### 3. 运行服务器
本项目使用.NET 10((((
如果你连如何打包都不会，那为啥要用这个项目？
如果你连如何调试运行都不会，那为啥要用这个项目？

## 许可证

本项目基于 AGPL-3.0 许可证开源。

## 贡献

欢迎提交 Issue 和 Pull Request 来改进这个项目。
