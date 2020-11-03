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

    let intToDigits baseVal number : int list = 
        let rec loop value digitArr = 
            let quotient = value / baseVal
            let remainder = value % baseVal
            if quotient = 0 then remainder :: digitArr
            else loop quotient (remainder :: digitArr)
        loop number []

    let numToBase(number: int, length: int, baseVal: int) = 
        let mutable result: string = 
            number
            |> intToDigits baseVal
            |> List.fold (fun acc x ->  acc + x.ToString()) ""
        
        let numZeroesToAppend = length - (String.length result)
        if numZeroesToAppend > 0 then
            let zeroesStr = String.init numZeroesToAppend (fun i ->   "0")
            result <- zeroesStr + result
        
        result