using System;
using System.Threading.Tasks;

namespace Spider
{
    class Program
    {



        static void Main(string[] args)
        {
            Core.Log4Net.LogInfo("爬虫已启动");

            string WeiboUrl = "";
            Console.Write("请输入要抓取的微博链接地址：");
            WeiboUrl = Console.ReadLine();
            if (string.IsNullOrEmpty(WeiboUrl))
            {
                WeiboUrl = "https://weibo.com/2714280233/JjGOwg75A?type=comment#_rnd1612691920314";
                Console.WriteLine($"默认链接{WeiboUrl}");
            }
            Console.Write("要保存的文件名：");
            string filename = Console.ReadLine();
            if (string.IsNullOrEmpty(filename))
            {
                filename = $"{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                Console.WriteLine($"默认文件名{filename}");
            }

            if (!filename.Contains(".xls"))
            {
                filename += ".xlsx";
            }

            var spider = new Core.Spider($"{WeiboUrl}", filename);

            Task.Run(() => spider.Run());
            Task.Run(() => spider.SaveComments());
            Console.WriteLine("程序正在后台处理，如果长时间没有输出，请关闭窗口后重新打开 ");
            Console.ReadLine();
        }
    }
}
