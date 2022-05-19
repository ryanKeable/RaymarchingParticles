using UnityEngine;
using System.Collections.Generic;

// this responder handles things like 
public sealed class Randomness : MonoBehaviour
{
    public static void random(Command theCommand)
    {
        float lowerRange = theCommand.floatForArg(1);
        float upperRange = theCommand.floatForArg(2);
        string varKey = theCommand[3];
        float random = MightyRandom.Range(lowerRange, upperRange);
        EnvironmentController.addEnvironmentValueForKey(random.ToString(), varKey);
    }

    public static void roll(Command theCommand)
    {
        if (theCommand.argCount != 3) {
            theCommand.setError("syntax error");
            return;
        }
        int rollValue = rollWithDice(theCommand[1], theCommand[2]);
        EnvironmentController.addEnvironmentValueForKey(rollValue.ToString(), theCommand[2]);
    }

    // Use this for initialization
    public static int rollWithDice(string diceString, string envKey)
    {
        // dice in the form: xdy+z
        string smashed = diceString.Replace(" ", "").ToLower(); // take out the spaces make it lowercase
        int total = 0;
        int numberOf = numberOfRolls(smashed);
        int sides = diceSides(smashed);

        for (int i = 0; i < numberOf; i++) {
            int thisRoll = MightyRandom.Range(0, sides) + 1;
            EnvironmentController.addEnvironmentValueForKey(thisRoll.ToString(), envKey + "_" + i);

            total += thisRoll;
        }

        total += bonus(smashed);

        return total;
    }

    // this is kinda a total hack
    public static int bonus(string diceString)
    {
        // dice in the form: xdy+z  or xdy-z
        if (diceString.Contains("+")) {
            return MathfExtensions.parseInt(diceString.Substring(diceString.IndexOf('+')));
        }
        if (diceString.Contains("-")) {
            return MathfExtensions.parseInt(diceString.Substring(diceString.IndexOf('-')));
        }
        return 0;
    }

    // this is kinda a total hack
    public static int numberOfRolls(string diceString)
    {
        // dice in the form: xdy+z  or xdy-z
        if (diceString.Contains("d")) {
            return MathfExtensions.parseInt(diceString.Substring(0, diceString.IndexOf('d')));
        }
        return 0;
    }

    // this is kinda a total hack
    public static int diceSides(string diceString)
    {
        // dice in the form: xdy+z  or xdy-z
        if (diceString.Contains("d")) {
            string diceSideWithBonus = diceString.Substring(diceString.IndexOf('d') + 1);
            if (diceSideWithBonus.Contains("+")) {
                return MathfExtensions.parseInt(diceSideWithBonus.Substring(0, diceSideWithBonus.IndexOf('+')));
            }
            if (diceSideWithBonus.Contains("-")) {
                return MathfExtensions.parseInt(diceSideWithBonus.Substring(0, diceSideWithBonus.IndexOf('-')));
            }
            return MathfExtensions.parseInt(diceSideWithBonus);
        }
        return 0;
    }
}
