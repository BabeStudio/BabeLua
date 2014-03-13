﻿#region License
/* **********************************************************************************
 * Copyright (c) Roman Ivantsov
 * This source code is subject to terms and conditions of the MIT License
 * for Irony. A copy of the license can be found in the License.txt file
 * at the root of this distribution. 
 * By using this source code in any fashion, you are agreeing to be bound by the terms of the 
 * MIT License.
 * You must not remove this notice from this software.
 * **********************************************************************************/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Irony.Parsing {
  /* 
    A node for a parse tree (concrete syntax tree) - an initial syntax representation produced by parser.
    It contains all syntax elements of the input text, each element represented by a generic node ParseTreeNode. 
    The parse tree is converted into abstract syntax tree (AST) which contains custom nodes. The conversion might 
    happen on-the-fly: as parser creates the parse tree nodes it can create the AST nodes and puts them into AstNode field. 
    Alternatively it might happen as a separate step, after completing the parse tree. 
    AST node might optinally implement IAstNodeInit interface, so Irony parser can initialize the node providing it
    with all relevant information. 
    The ParseTreeNode also works as a stack element in the parser stack, so it has the State property to carry 
    the pushed parser state while it is in the stack. 
  */
  public class ParseTreeNode {
    public object AstNode;
    public Token Token; 
    public BnfTerm Term;
    public int Precedence;
    public Associativity Associativity;
    public SourceSpan Span;
    //Making ChildNodes property (not field) following request by Matt K, Bill H
    public ParseTreeNodeList ChildNodes {get; private set;}
    public bool IsError;
    internal ParserState State;      //used by parser to store current state when node is pushed into the parser stack
    public object Tag; //for use by custom parsers, Irony does not use it
    public TokenList Comments; //Comments preceding this node

    private ParseTreeNode(){
      ChildNodes = new ParseTreeNodeList();
    }

    public ParseTreeNode(Token token) : this()  {
      Token = token;
      Term = token.Terminal;
      Precedence = Term.Precedence;
      Associativity = token.Terminal.Associativity;
      Span = new SourceSpan(token.Location, token.Length);
      IsError = token.IsError(); 
    }

    public ParseTreeNode(ParserState initialState) : this() {
      State = initialState;
    }

    public ParseTreeNode(NonTerminal term, SourceSpan span)  : this(){
      Term = term;
      Span = span; 
    }
    
    public override string ToString() {
      if (Term == null) 
        return "(S0)"; //initial state node
      else 
        return Term.GetParseNodeCaption(this); 
    }//method


    public string FindTokenAndGetText() {
      var tkn = FindToken();
      return tkn == null ? null : tkn.Text;       
    }
    public Token FindToken() {
      return FindFirstChildTokenRec(this); 
    }
    private static Token FindFirstChildTokenRec(ParseTreeNode node) {
      if (node.Token != null) return node.Token;
      foreach (var child in node.ChildNodes) {
        var tkn = FindFirstChildTokenRec(child);
        if (tkn != null) return tkn; 
      }
      return null; 
    }

    /// <summary>Returns true if the node is punctuation or it is transient with empty child list.</summary>
    /// <returns>True if parser can safely ignore this node.</returns>
    public bool IsPunctuationOrEmptyTransient() {
      if (Term.Flags.IsSet(TermFlags.IsPunctuation))
        return true;
      if (Term.Flags.IsSet(TermFlags.IsTransient) && ChildNodes.Count == 0)
        return true;
      return false; 
    }

    public bool IsOperator() {
      return Term.Flags.IsSet(TermFlags.IsOperator);
    }

  }//class

  public class ParseTreeNodeList : List<ParseTreeNode> { }

  public enum ParseTreeStatus {
    Parsing,
    Partial,
    Parsed,
    Error,
  }

  public class ParseTree {
    public ParseTreeStatus Status {get; internal set;}
    public readonly string SourceText;
    public readonly string FileName; 
    public readonly TokenList Tokens = new TokenList();
    public readonly TokenList OpenBraces = new TokenList(); 
    public ParseTreeNode Root;
    public readonly LogMessageList ParserMessages = new LogMessageList();
    public long ParseTimeMilliseconds;
    public object Tag; //custom data object, use it anyway you want

    public ParseTree(string sourceText, string fileName) {
      SourceText = sourceText;
      FileName = fileName;
      Status = ParseTreeStatus.Parsing;
    }

    public bool HasErrors() {
      if (ParserMessages.Count == 0) return false;
      foreach (var err in ParserMessages)
        if (err.Level == ErrorLevel.Error) return true;
      return false; 
    }//method

  }//class

}
