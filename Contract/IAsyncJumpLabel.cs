using ICSharpCode.NRefactory.CSharp;
using System;
using System.Text;
namespace Bridge.Contract
{
    public interface IAsyncJumpLabel
    {
        StringBuilder Output { get; set; }
        AstNode Node { get; set; }
    }
}
