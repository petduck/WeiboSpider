using System;
using System.Collections.Generic;
using System.Text;

namespace Spider.Models
{
    public class Response
    {
        public string code { get; set; }

        public string msg { get; set; }

        public CommentList data { get; set; }

        public class CommentList
        {
            public string count { get; set; } = "0";

            public string html { get; set; }

            public string tag { get; set; }

            public PageModel page { get; set; }
        }

        public class PageModel
        {
            public int totalpage { get; set; } = 0;

            public int pagenum { get; set; } = 1;
        }
    }


}
