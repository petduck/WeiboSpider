using AngleSharp;
using AngleSharp.Dom;
using Microsoft.Extensions.PlatformAbstractions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Core
{
    public class Spider
    {
        IBrowsingContext context;
        List<Models.Comment> comments;
        private string cookies = "";    //cookie
        private HttpService httpService;
        private bool exit = false;
        string weibo_url = "";
        private Excel excel;
        private string excel_path;

        public Spider(string _url,string filename)
        {
            context = BrowsingContext.New(Configuration.Default);
            comments = new List<Models.Comment>();
            weibo_url = _url;

            var uri = new Uri(weibo_url);
            cookies = System.Configuration.ConfigurationManager.AppSettings["cookie"];

            var httpClient = new HttpClient()
            {
                BaseAddress = new Uri($"{uri.Scheme}://{uri.Host}"),
                Timeout = new TimeSpan(0, 0, 30),                
            };

            string filedir= System.Configuration.ConfigurationManager.AppSettings["filedir"];
            excel_path = $"{PlatformServices.Default.Application.ApplicationBasePath}{filedir}/out/{filename}";            
            excel = new Excel(excel_path);
            Log4Net.LogInfo($"Excel文件保存在{excel_path}");
            httpService = new HttpService(httpClient);
        }

        public async Task Run()
        {
            string res_str = "";
            try
            {
                Log4Net.LogInfo($"正在抓取微博[{weibo_url}]的评论");
                Uri Weibo_Uri = new Uri(weibo_url);
                res_str = await httpService.GetAsync(Weibo_Uri.PathAndQuery, cookies);
                if (!string.IsNullOrEmpty(res_str))
                {
                    var document = await context.OpenAsync(req => req.Content(res_str));
                    var scripts = document.Scripts;
                    if (scripts.Length > 0)
                    {
                        var js = scripts.Where(x => x.InnerHtml.Contains(@"""ns"":""pl.content.weiboDetail.index""", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                        if (js != null)
                        {
                            var txt = js.TextContent;
                            if (txt != null)
                            {
                                string json_str = txt.Replace("FM.view(", "").TrimEnd(')');

                                var json = JsonConvert.DeserializeObject<dynamic>(json_str);

                                var temp_doc = await context.OpenAsync(req => req.Content(Convert.ToString(json.html)));
                                var weibo_link_dom = temp_doc.QuerySelectorAll("*").Where(x => "feed_list_commentTabAll".Equals(x.GetAttribute("node-type")));
                                if (weibo_link_dom.Any())
                                {
                                    string weibo_link = weibo_link_dom.FirstOrDefault().GetAttribute("action-data");
                                    if (!string.IsNullOrEmpty(weibo_link))
                                    {                                        
                                        string url = $"{Link(weibo_link)}";
                                        Log4Net.LogInfo($"获得评论入口URL[{url}]");
                                        Uri uri = new Uri(url);
                                        Log4Net.LogInfo($"开始抓取评论");
                                        await CommentHandle(uri, weibo_link);  //处理评论
                                    }
                                }
                            }
                        }
                        
                    }
                }
            }
            catch (Exception e)
            {
                Log4Net.ErrorInfo($"处理微博[{weibo_url}]异常", e);
                Log4Net.LogInfo($"本次异常的字符串：{res_str}");
            }
            exit = true;
        }

        /// <summary>
        /// 处理评论
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        private async Task CommentHandle(Uri uri,string weibo_link)
        {
            Log4Net.LogInfo($"正在抓取评论链接[{uri}]");
            var res_str = await httpService.GetAsync(uri.PathAndQuery, cookies);
            if (!string.IsNullOrEmpty(res_str))
            {
                try
                {
                    Log4Net.LogInfo($"评论链接[{uri}]内容获取成功，开始处理数据");
                    var response = JsonConvert.DeserializeObject<Models.Response>(res_str);

                    var document = await context.OpenAsync(req => req.Content(response.data.html));
                    var document_comment_list = document.QuerySelectorAll("*").Where(x => "comment_list".Equals(x.GetAttribute("node-type"))).FirstOrDefault(); //获取评论列表的dom
                    Log4Net.LogInfo($"取得评论区");
                    //这里有：主评论、子评论列表的链接、主评论下一页的链接
                    //1.取主评论列表

                    document_comment_list
                        .QuerySelectorAll("div")
                        .Where(x=> "list_li S_line1 clearfix".Equals(x.ClassName))
                        .ToList()   //遍历主评论
                        .ForEach(x =>
                        {
                            var commentId = x.GetAttribute("comment_id");   //主评论Id
                            var comment_body_dom = x.GetElementsByClassName("WB_text").FirstOrDefault();    //评论主体DOM
                                                                                                            //从这里取用户Id、昵称和评论内容，图片和下级子评论在别的节点中取
                                                                                                            //1.取用户Id，第1个A标签中有用户Id和昵称
                            var a_dom = comment_body_dom.QuerySelectorAll("a");
                            var first_a_dom = a_dom.FirstOrDefault();
                            var user_id = first_a_dom.GetAttribute("usercard").Replace("id=", "");  //用户Id
                            var nick = first_a_dom.TextContent; //直接取文字部分为昵称
                            var datetime = x.GetElementsByClassName("WB_from S_txt2").FirstOrDefault().TextContent;

                            foreach (var a in a_dom)
                            {   //删除所有的A标签
                                a.Remove();
                            }

                            //2.评论的内容，处理表情，先取表情的图片
                            comment_body_dom.QuerySelectorAll("img").Where(m => "face".Equals(m.GetAttribute("type"))).ToList().ForEach(face =>
                            {
                                 comment_body_dom.InnerHtml = comment_body_dom.InnerHtml.Replace(face.OuterHtml, face.GetAttribute("title"));    //把表情图片替换成文字
                            });

                            var body = comment_body_dom.TextContent.Trim().TrimStart('：'); //评论内容

                            var comment = new Models.Comment
                            {
                                CommentId = commentId,
                                UserId = user_id,
                                Nick = nick,
                                DateAndTime = datetime,
                                Body = body,
                                CommentUrl=$"{uri}"
                            };

                            //3.处理评论图片，单图
                            var image_dom = x.QuerySelectorAll("div").Where(m => "comment_media_prev".Equals(m.GetAttribute("node-type")));
                            if (image_dom.Any())
                            {
                                comment.ImageUrl = CommentImageHandle(image_dom.FirstOrDefault());
                            }

                            comments.Add(comment);
                        });
                    string link = "";
                    var page = response.data.page;
                    if (page != null)
                    {
                        Log4Net.LogInfo($"处理完第[{page.pagenum}]页评论数据");
                        if (page.pagenum < page.totalpage)
                        {
                            link = $"{Link(weibo_link)}&page={(page.pagenum + 1)}";
                        }
                    }
                    else
                    {
                        link = await NextLinkHandle(document_comment_list);
                        link = $"{Link(link)}";
                    }
                    if (!string.IsNullOrEmpty(link))
                    {
                        Log4Net.LogInfo($"5秒后处理URL[{link}]的评论");
                        uri = new Uri(link);
                        System.Threading.Thread.Sleep(5000);
                        await CommentHandle(uri,weibo_link);
                    }
                    else
                    {
                        exit = true;    //退出
                        Log4Net.LogInfo($"评论数据处理完成");
                        Log4Net.LogInfo($"最后一页内容：{res_str}");
                    }
                }
                catch(Exception e)
                {
                    Log4Net.ErrorInfo($"处理评论数据异常，URL：[{uri}]，网页返回：{res_str}",e);
                    Log4Net.LogInfo($"5秒后异常重试URL[{uri}]");
                    System.Threading.Thread.Sleep(5000);
                    await CommentHandle(uri, weibo_link);
                }
            }
        }

        /// <summary>
        /// 图片评论的图片
        /// </summary>
        /// <param name="Comment_Image_DOM"></param>
        /// <returns></returns>
        private string CommentImageHandle(AngleSharp.Dom.IElement Comment_Image_DOM)
        {
            var image_src = Comment_Image_DOM.QuerySelectorAll("img").FirstOrDefault().GetAttribute("src");
            if (!string.IsNullOrEmpty(image_src))
            {
                //替换地址为原图地址
                image_src = image_src.Replace(@"/thumb180/", @"/bmiddle/");
            }

            return image_src;
        }

        /// <summary>
        /// 处理下一页链接
        /// </summary>
        /// <returns></returns>
        private async Task<string> NextLinkHandle(AngleSharp.Dom.IElement dom)
        {
            string link = "";

            //这里可能会有多种分页形式，需要不同情况不同判断
            //1.加载更多
            var divs_dom = dom.QuerySelectorAll("*");
            if (divs_dom.Any())
            {
                foreach (var x in divs_dom)
                {
                    if ("comment_loading".Equals(x.GetAttribute("node-type")) || "click_more_comment".Equals(x.GetAttribute("action-type")) || "click_more_child_comment_big".Equals(x.GetAttribute("action-type")))
                    {
                        link = x.GetAttribute("action-data");
                        break;
                    }
                }
            }

            return await Task.FromResult(link);
        }

        private string Link(string id)
        {
            return $"https://weibo.com/aj/v6/comment/big?ajwvr=6&{id.Replace("filter=0", "filter=all").Replace("filter=hot", "filter=all")}&__rnd={Convert.ToInt64(DateTime.Now.Ticks / 10000000)}";
        }

        /// <summary>
        /// 保存评论到Excel
        /// </summary>
        /// <returns></returns>
        public void SaveComments()
        {
            int count = 0;
            while (true)
            {
                if (comments.Count > 0)
                {
                    var list = comments.Select(x => x).ToList();
                    //保存到excel中
                    try
                    {
                        comments.RemoveAll(x => { return list.Any(i => i.Equals(x)); });
                        Log4Net.LogInfo($"本次保存{list.Count}条评论");
                        excel.Insert(list);
                        count++;
                    }
                    catch (Exception e)
                    {
                        Log4Net.ErrorInfo($"本次保存到Excel失败，行数：{list.Count}，值：{JsonConvert.SerializeObject(list)}。异常信息：", e);
                    }

                }
                else
                {
                    if (exit)
                    {
                        Log4Net.LogInfo($"数据导出完成，保存Excel文件");
                        excel.Save();
                        Log4Net.LogInfo($"本次任务已完成，程序退出");
                        break;
                    }
                }
                if (count / 10 == 0 && count > 0)
                {   //每插入10次数据，保存一次Excel
                    excel.Save();
                }
                System.Threading.Thread.Sleep(5000);
            }
        }

    }
}
