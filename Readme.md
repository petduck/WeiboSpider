# 说明
## 一、简述
该代码实例实现了微博评论的简单抓取，并把抓取到的信息保存到Excel中。HTML解析使用的是AngleSharp库，Excel导出使用的是ClosedXML.Excel库。

## 二、使用方法
### 1.配置
配置放在App.config中，目前只支持两项配置，是导出的Excel文件的保存目录，另一个是Cookies。Cookies的获取方法：<br/>
	1.使用Firefox或Chrome浏览器打开微博网站，按`F12`打开`开发者模式`，之后切换到`网络`或`Network`选项卡，找到Cookie。
	2.把找到的Cookie添加到App.config中的`cookie`项中。

### 2.执行
在Windows中运行`Spider.exe`文件，或在Linux下执行`dotnet Spider.dll`，按提示信息填写要抓取评论的微博地址和要保存的文件名，如果不填将使用默认值。抓取结束后Excel将保存到设定的文件夹下的`out`目录中，日志文件保存在`logfiles`文件夹下。

## 三、其他
目前只支持一级评论，不支持子评论。因为微博接口限制，有时抓取的数据会特别少，多试几次。
