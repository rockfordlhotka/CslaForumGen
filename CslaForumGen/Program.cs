using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using System.IO;

namespace CslaForumGen
{
  class Program
  {
    static string _folder;

    static void Main(string[] args)
    {
      _folder = args[0];
      if (string.IsNullOrWhiteSpace(_folder))
        throw new ArgumentException("folder");
      if (_folder.EndsWith("\\"))
        _folder = _folder.Substring(0, _folder.Length - 1);

      var cstr = "Data Source=ROCKYDESKTOP;Initial Catalog=CslaForum;Integrated Security=True";
      var cn = new SqlConnection(cstr);
      cn.Open();
      var threads = cn.Query<Post>("select ThreadId, Subject, PostDate from cs_Posts where ParentId=PostId order by ThreadId desc").ToList();
      Console.WriteLine($"Processing {threads.Count()} threads");
      var file = File.CreateText($"{_folder}\\index.html");
      try
      {
        file.Write($"<html><header><title>CSLA Forum Archive Index</title></header><body>");
        file.Write($"<div style='float:right'><a href='http://cslanet.com'><img src='https://github.com/MarimerLLC/csla/raw/master/Support/Logos/csla%20win8_compact_s.png'/></a></div>");
        file.Write($"<h1>CSLA Forum Archive Index</h1>");
        file.Write($"<a href='http://cslanet.com'>CSLA .NET home page</a><br>");
        file.Write($"<a href='https://github.com/marimerllc/cslaforum'>Current CSLA .NET forum</a>");
        file.Write($"<hr>");

        int count = 0;
        string bcolor;
        foreach (var item in threads)
        {
          count++;
          if (count > 1) count = 0;
          if (count == 0)
            bcolor = "white";
          else
            bcolor = "powderblue";

          file.Write($"<div style='background-color:{bcolor}'>{item.PostDate.ToString("dd MMM yyyy")} - <a href='{item.ThreadID}.html'>{item.Subject}</a></div>");
          WriteThread(cn, item.ThreadID);
          Console.Write(".");
        }
      }
      finally
      {
        file.Close();
      }
    }

    private static void WriteThread(SqlConnection cn, int threadId)
    {
      var file = File.CreateText($"{_folder}\\{threadId}.html");
      try
      {
        var posts = cn.Query<Post>("select * from cs_Posts where ThreadId = @tid order by ThreadID, SortOrder", new { tid = threadId });
        if (posts.Count() > 0)
        {
          var first = posts.First();
          file.Write($"<html><header><title>{first.Subject}</title></header><body>");
          file.Write($"<div style='float:right'><a href='http://cslanet.com'><img src='https://github.com/MarimerLLC/csla/raw/master/Support/Logos/csla%20win8_compact_s.png'/></a></div>");
          file.Write($"<p><h1>{first.Subject}</h1>");
          file.Write($"<span style='font-size:small'>Old forum URL: forums.lhotka.net/forums/t/{first.ThreadID}.aspx</span></p><hr>");
          foreach (var item in posts)
          {
            var bcolor = "powderblue";
            if (!string.IsNullOrEmpty(item.PropertyNames) && item.PropertyNames.StartsWith("Answer"))
              bcolor = "lightgreen";
            file.Write($"<div style='padding:0 15 3 15;background-color:{bcolor}'><h2>");
            if (item.ParentID == item.PostID)
              file.Write($"{item.PostAuthor} posted");
            else
              file.Write($"{item.PostAuthor} replied");
            file.Write($" on {item.PostDate.ToLongDateString()}</h2>");
            file.Write($"{FixQuotes(item.FormattedBody)}");
            file.Write($"</div>");
          }
          file.Write("<p style='font-size:small'>Copyright (c) Marimer LLC");
          file.WriteLine("</body></html>");
        }
      }
      finally
      {
        file.Close();
      }
    }

    private static string FixQuotes(string body)
    {
      var start = body.IndexOf("[quote");
      if (start >=0)
      {
        var end = body.IndexOf("]", start);
        var quote = body.Substring(start, end - start + 1);
        var name = quote.Replace("[quote user=&quot;", "");
        name = name.Replace("&quot;]", "<br>");
        body = body.Replace(quote, $"<div style='padding-left: 50px;background-color:silver'><b>{name}</b>");
        body = body.Replace("[/quote]", "</div>");
      }
      return body;
    }
  }

  public class Post
  {
    public int PostID { get; set; }
    public int ThreadID { get; set; }
    public int ParentID { get; set; }
    public string PostAuthor { get; set; }
    public int SortOrder { get; set; }
    public string Subject { get; set; }
    public DateTime PostDate { get; set; }
    public string Body { get; set; }
    public string FormattedBody { get; set; }
    public string PropertyNames { get; set; }
  }
}
