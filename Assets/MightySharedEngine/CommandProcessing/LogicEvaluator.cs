using UnityEngine;
using System;
using System.Collections.Generic;
using System.Globalization;

public sealed class LogicEvaluator
{
    public Func<string, int> intForVariable;

    public LogicEvaluator()
    {
        // set the default variable function
        intForVariable = (string theToken) => {
            MDebug.Log("CANNOT PARSE TOKEN: " + theToken);
            return 0;
        };
    }

    public int evaluateString(string raw, Func<string, int> variableFunction)
    {
        intForVariable = variableFunction;
        return evaluateString("(" + raw + ")");
    }

    public int evaluateString(string raw)
    {
        List<String> tokens = tokenize(raw);
        List<String> postfix = convertToPostfix(tokens);
        int expression = evaluateExpression(postfix);
        return expression;
    }

    public string addParenthesis(string raw)
    {
        if (raw.Contains("("))
            return raw; // we will presume that there are already parens
        raw = addParenthesis(raw, "&");
        raw = addParenthesis(raw, "|");
        return raw;
    }

    public string addParenthesis(string raw, string seperator)
    {
        // now add some enclosing ( and ) for the & and |
        string[] chopBits = raw.Split(seperator[0]);
        if (chopBits.Length == 1)
            return raw;

        string modString = "(";
        for (int i = 0; i < chopBits.Length; i++) {
            if (i == chopBits.Length - 1) {
                modString += chopBits[i] + ")";
            } else {
                modString += chopBits[i] + ") " + seperator + " (";
            }
        }
        return modString;
    }

    public List<string> tokenize(string raw)
    {
        // we are going to scan through the raw string one character at a time and
        // build a list of all the tokens
        // first we are going to convert our two character operators into single character operators
        // this is just temporary to make the parse simpler, it is probably a shitty idea
        string parenthesised = addParenthesis(raw);

        string mangled = parenthesised.Replace(">=", "$").Replace("<=", "#").Replace("!=", "~");

        string operators = "+-*/&|$#~><=";
        // we will use some slow-ass string concatting here, if this turns out to suck
        // too hard we will need to convert it to something else
        List<string> tokens = new List<string>();

        string thisToken = "";

        for (int i = 0; i < mangled.Length; i++) {
            char thisCharacter = mangled[i];

            if (thisCharacter == '-') { // check for unary
                if (tokens.Count == 0) {
                    thisToken = thisToken + thisCharacter;
                    continue;
                }
                if (isOperator(tokens[tokens.Count - 1]) || tokens[tokens.Count - 1] == "(") {
                    thisToken = thisToken + thisCharacter;
                    continue;
                }
            }
            if (thisCharacter == ' ') {
                if (thisToken.Length > 0) {
                    tokens.Add(String.Copy(thisToken));
                    thisToken = "";
                }
                continue;
            }
            if (operators.IndexOf(thisCharacter) >= 0 || thisCharacter == '(' || thisCharacter == ')') {
                if (thisToken.Length > 0) {
                    tokens.Add(String.Copy(thisToken));
                }
                thisToken = "" + thisCharacter;
                if (thisCharacter == '$' || thisCharacter == '#' || thisCharacter == '~') {
                    thisToken = thisToken.Replace("$", ">=").Replace("#", "<=").Replace("~", "!=");
                }
                tokens.Add(String.Copy(thisToken));
                thisToken = "";
                continue;
            }
            thisToken = thisToken + thisCharacter;
        }
        if (thisToken.Length > 0) {
            tokens.Add(String.Copy(thisToken));
        }

        return tokens;
    }

    /*
    While there are tokens to be read:
    Read a token.
    If the token is a number, then add it to the output queue.

    If the token is an operator, o1, then:
    while there is an operator token, o2, at the top of the stack, and
    either o1 is left-associative and its precedence is equal to that of o2,
    or o1 has precedence less than that of o2,
    pop o2 off the stack, onto the output queue;
    push o1 onto the stack.
    If the token is a left parenthesis, then push it onto the stack.
    If the token is a right parenthesis:
    Until the token at the top of the stack is a left parenthesis, pop operators off the stack onto the output queue.
    Pop the left parenthesis from the stack, but not onto the output queue.
    If the token at the top of the stack is a function token, pop it onto the output queue.
    If the stack runs out without finding a left parenthesis, then there are mismatched parentheses.
    When there are no more tokens to read:
    While there are still operator tokens in the stack:
    If the operator token on the top of the stack is a parenthesis, then there are mismatched parentheses.
    Pop the operator onto the output queue.
    Exit.
    */
    // go here: http://en.wikipedia.org/wiki/Shunting_yard_algorithm
    public List<string> convertToPostfix(List<string> tokens)
    {
        List<string> outputQueue = new List<string>();
        Stack<string> theStack = new Stack<string>();
        for (int i = 0; i < tokens.Count; i++) {
            string theToken = tokens[i];
            if (isOperator(theToken)) {
                while (theStack.Count > 0 && isOperator(theStack.Peek()) && precedenceLessThanOrEqual(theToken, theStack.Peek())) {
                    outputQueue.Add(theStack.Pop());
                }
                theStack.Push(theToken);
                continue;
            }
            if (theToken == "(") {
                theStack.Push(theToken);
                continue;
            }
            if (theToken == ")") {
                while (theStack.Count > 0 && theStack.Peek() != "(") {
                    outputQueue.Add(theStack.Pop());
                }
                theStack.Pop();
                continue;
            }
            // just a number
            outputQueue.Add(theToken);
        }
        while (theStack.Count > 0) {
            outputQueue.Add(theStack.Pop());
        }
        return outputQueue;
    }

    public bool precedenceLessThanOrEqual(string op1, string op2)
    {
        int precedence1 = operatorPrecedence(op1[0]);
        int precedence2 = operatorPrecedence(op2[0]);
        if (precedence1 <= precedence2)
            return true;
        return false;
    }

    public int operatorPrecedence(char theOperator)
    {
        if (theOperator == '&' || theOperator == '|')
            return 4;
        if (theOperator == '<' || theOperator == '>')
            return 3;
        if (theOperator == '+' || theOperator == '-')
            return 1;
        return 2;
    }

    // basically the idea here is to push stuff onto the stack until you get to an operator
    // then pop off the last two things on the stack and apply the operator and push the result
    // back onto the stack
    // for our purposes logical expressions will be either 1 or 0;
    public int evaluateExpression(List<string> tokens)
    {
        Stack<string> theStack = new Stack<string>();

        for (int i = 0; i < tokens.Count; i++) {
            string theToken = tokens[i];
            if (isOperator(theToken)) {
                if (theStack.Count < 2) {
                    MDebug.Log("Cannot evaluate expression. Syntax Error.");
                    return 0; // error!
                }
                int value2 = evaluateToken(theStack.Pop());
                int value1 = evaluateToken(theStack.Pop());
                int value3 = evaluateOperation(value1, value2, theToken);
                theStack.Push(value3.ToString());
                continue;
            }
            theStack.Push(theToken);
        }
        return evaluateToken(theStack.Peek());
    }

    public int evaluateOperation(int value1, int value2, string operation)
    {
        if (operation == "+")
            return value1 + value2;
        if (operation == "-")
            return value1 - value2;
        if (operation == "*")
            return value1 * value2;
        if (operation == "/")
            return value1 / value2;
        if (operation == "<")
            return (value1 < value2) ? 1 : 0;
        if (operation == ">")
            return (value1 > value2) ? 1 : 0;
        if (operation == "<=")
            return (value1 <= value2) ? 1 : 0;
        if (operation == ">=")
            return (value1 >= value2) ? 1 : 0;
        if (operation == "&")
            return ((value1 != 0) && (value2 != 0)) ? 1 : 0;
        if (operation == "|")
            return ((value1 != 0) || (value2 != 0)) ? 1 : 0;
        if (operation == "=")
            return (value1 == value2) ? 1 : 0;
        if (operation == "!=")
            return (value1 != value2) ? 1 : 0;
        return 0;
    }

    public bool isOperator(string theToken)
    {
        string theOperators = "+-*/&|$#~>=<=";
        if (theOperators.Contains(theToken))
            return true;
        return false;
    }

    public int evaluateToken(string theToken)
    {
        int tryToParseInt = 0;
        if (int.TryParse(theToken, NumberStyles.Any, CultureInfo.InvariantCulture, out tryToParseInt)) {
            return tryToParseInt;
        }
        int tokenValue = intForVariable(theToken);
        return tokenValue;
    }
}
