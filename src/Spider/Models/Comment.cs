using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Spider.Models
{
    /// <summary>
    /// 评论
    /// </summary>
    public class Comment
    {
        /// <summary>
        /// 评论Id
        /// </summary>
        [Description("评论Id")]
        public string CommentId { get; set; }

        /// <summary>
        /// 用户昵称
        /// </summary>
        [Description("用户昵称")]
        public string Nick { get; set; }

        /// <summary>
        /// 用户Id
        /// </summary>
        [Description("用户Id")]
        public string UserId { get; set; }

        /// <summary>
        /// 发布时间
        /// </summary>
        [Description("发布时间")]
        public string DateAndTime { get; set; }

        /// <summary>
        /// 评论内容
        /// </summary>
        [Description("评论内容")]
        public string Body { get; set; }

        /// <summary>
        /// 评论图片地址
        /// </summary>
        [Description("评论图片地址")]
        public string ImageUrl { get; set; }

        /// <summary>
        /// 评论所在地址
        /// </summary>
        [Description("评论所在地址")]
        public string CommentUrl { get; set; }
    }
}
