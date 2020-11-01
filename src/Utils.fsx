namespace Utils

open System

module Utils = 
    // Utility methods
    let strToInt (str: string) = 
        str |> int

    let toDouble int = 
        int |> double

    let logOf =
        toDouble >> Math.Log

    // x^y
    let powOf (x: int, y: int) = 
        Math.Pow (toDouble x, toDouble y)