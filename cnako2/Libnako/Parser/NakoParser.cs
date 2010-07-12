﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Libnako.Parser
{
    /// <summary>
    /// トークンを読み込んで構文木に変換するクラス
    /// </summary>
    public class NakoParser : NakoParserBase
    {
        public NakoParser(NakoTokenList tokens) : base(tokens)
        {
        }

        public Boolean ParseOnlyValue()
        {
            lastNode = null;
            if (tok.IsEOF()) return false;
            if (!_value()) return false;
            topNode.AddChild(lastNode);
            return true;
        }

        // top : empty | _block ;
        public void Parse()
        {
            while (true)
            {
                if (tok.IsEOF()) break;
                if (!_block()) break;
                tok.MoveNext();
            }
        }

        // _block : empty | _statement ;
        private Boolean _block()
        {
            if (tok.IsEOF()) return true;
            return _statement();
        }

        // _statement : _let
        private Boolean _statement()
        {
            return _let();
        }

        // _let : _variable T_EQ _value
        private Boolean _let()
        {
            tok.Save();
            if (!_variable())
            {
                tok.Restore();
                return false;
            }
            NakoNodeLet node = new NakoNodeLet();
            node.nodeVar = lastNode;
 
            if (!Accept(TokenType.T_EQ))
            {
                tok.Restore();
                return false;
            }
            tok.MoveNext();

            if (!_value())
            {
                throw new NakoParserExcept("代入文で値がありません。");
            }
            node.nodeValue = lastNode;

            parentNode.AddChild(node);

            tok.RemoveTop();
            return true;
        }

        // _variable : T_WORD '[' T_VALUE ']'
        //           | T_WORD
        private Boolean _variable()
        {
            if (!Accept(TokenType.T_WORD)) return false;
            
            // 配列アクセス
            if (tok.NextTokenType == TokenType.T_BLACKETS_L)
            {
                // TODO
                throw new NakoParserExcept("Not supported");
            }

            // 普通の変数アクセス
            NakoNode n = new NakoNode();
            n.type = NodeType.N_VARIABLE;
            n.Token = tok.CurrentToken;
            lastNode = n;
            tok.MoveNext();
            return true;
        }

        // _value : _calc_formula | _calc_value ;
        private Boolean _value()
        {
            if (_calc_fact()) return true;
            return false;
        }

        // _calc_value : _const | _variable ;
        private Boolean _calc_value()
        {
            if (_const()) return true;
            if (_variable()) return true;
            return false;
        }

        // _calc_formula : T_PARENTHESES_L _value T_PARENTHESES_R 
        //               | _value
        private Boolean _calc_formula()
        {
            NakoNodeCalc node = new NakoNodeCalc();
            node.Token = tok.CurrentToken;
            
            // Check '(' *** ')'
            if (Accept(TokenType.T_PARENTHESES_L))
            {
                tok.Save();
                tok.MoveNext();
                if (!_value())
                {
                    tok.Restore();
                    return false;
                }
                if (Accept(TokenType.T_PARENTHESE_R))
                {
                    node.nodeL = this.lastNode;
                    tok.RemoveTop();
                    lastNode = node;
                    return true;
                }
                tok.Restore();
                return false;
            }

            return _calc_value();
        }

        // _calc_expr : _calc_formula T_MUL _calc_formula
        //            | _calc_formula T_DIV _calc_formula
        //            ;
        private Boolean _calc_expr()
        {
            NakoNodeCalc node = new NakoNodeCalc();
            if (!_calc_formula()) { return false; }
            node.nodeL = lastNode;
            if (Accept(TokenType.T_MUL) || Accept(TokenType.T_DIV))
            {
                tok.Save();
                node.calc_type = 
                    Accept(TokenType.T_MUL) ? NakoNodeCalc.CalcType.Mul
                                            : NakoNodeCalc.CalcType.Div;
                tok.MoveNext();
                if (!_calc_formula())
                {
                    tok.Restore();
                    return false;
                }
                tok.RemoveTop();
                node.nodeR = lastNode;
                lastNode = node;
                return true;
            }
            else
            {
                return true;
            }
        }

        // _calc_term : _calc_expr T_PLUS _calc_expr 
        //            | _calc_expr T_MINUS _calc_expr
        //            ;
        private Boolean _calc_term()
        {
            NakoNodeCalc node = new NakoNodeCalc();
            if (!_calc_expr())
            {
                return false;
            }
            node.nodeL = lastNode;
            if (Accept(TokenType.T_PLUS) || Accept(TokenType.T_MINUS))
            {
                node.calc_type =
                    Accept(TokenType.T_PLUS) ? NakoNodeCalc.CalcType.Plus
                                             : NakoNodeCalc.CalcType.Minus;
                tok.Save();
                tok.MoveNext();
                if (!_calc_expr())
                {
                    tok.Restore();
                    return false;
                }
                tok.RemoveTop();
                node.nodeR = lastNode;
                lastNode = node;

                return true;
            }
            else
            {
                return true;
            }
        }

        // _calc_comp : _calc_term T_GT _calc_term
        //            | _calc_term T_LT _calc_term
        //            ;
        private Boolean _calc_comp()
        {
            NakoNodeCalc node = new NakoNodeCalc();
            if (!_calc_term())
            {
                tok.Restore();
                return false;
            }

            node.nodeL = lastNode;
            if (Accept(TokenType.T_GT) ||
                Accept(TokenType.T_LT) ||
                Accept(TokenType.T_GT_EQ) ||
                Accept(TokenType.T_LT_EQ) ||
                Accept(TokenType.T_EQ) ||
                Accept(TokenType.T_NOT_EQ))
            {
                switch (tok.CurrentToken.type)
                {
                    case TokenType.T_GT: node.calc_type = NakoNodeCalc.CalcType.Gt; break;
                    case TokenType.T_LT: node.calc_type = NakoNodeCalc.CalcType.Lt; break;
                    case TokenType.T_GT_EQ: node.calc_type = NakoNodeCalc.CalcType.GtEq; break;
                    case TokenType.T_LT_EQ: node.calc_type = NakoNodeCalc.CalcType.LtEq; break;
                    case TokenType.T_EQ: node.calc_type = NakoNodeCalc.CalcType.Eq; break;
                    case TokenType.T_NOT_EQ: node.calc_type = NakoNodeCalc.CalcType.NotEq; break;
                }
                tok.Save();
                tok.MoveNext();
                if (!_calc_term())
                {
                    tok.Restore();
                    return false;
                }
                tok.RemoveTop();
                node.nodeR = lastNode;
                lastNode = node;
                return true;
            }
            else
            {
                return true;
            }
        }

        // _calc_fact : T_MINUS _calc_fact
        //            | _calc_comp
        //            ;
        private Boolean _calc_fact()
        {
            NakoNodeCalc node = new NakoNodeCalc();
            node.Token = tok.CurrentToken;

            if (Accept(TokenType.T_MINUS))
            {
                node.calc_type = NakoNodeCalc.CalcType.Neg;
                tok.MoveNext();
                if (!_calc_fact())
                {
                    throw new NakoParserExcept("「-」記号の後に値がありません。");
                }
                node.nodeL = lastNode;
                tok.RemoveTop();
                lastNode = node;
                return true;
            }

            if (_calc_comp())
            {
                return true;
            }

            return false;
        }
        
        // _const : T_INT | T_NUMBER | T_STRING ;
        private Boolean _const()
        {
            NakoNodeConst node = new NakoNodeConst();
            node.Token = tok.CurrentToken;

            if (Accept(TokenType.T_INT))
            {
                node.type = NodeType.N_INT;
                node.value = Int32.Parse(node.Token.value);
                lastNode = node;
                tok.MoveNext();
                return true;
            }
            else if (Accept(TokenType.T_NUMBER))
            {
                node.type = NodeType.N_NUMBER;
                node.value = Double.Parse(node.Token.value);
                lastNode = node;
                tok.MoveNext();
                return true;
            }
            else if (Accept(TokenType.T_STRING))
            {
                node.type = NodeType.N_STRING;
                node.value = node.Token.value;
                lastNode = node;
                tok.MoveNext();
                return true;
            }

            return false;
        }
    }
}
