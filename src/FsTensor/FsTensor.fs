module FsTensor
//module for the Tensor data structure and its operators

module Domain =
    type internal Tensor<'T> =
        {
            elements: 'T array
            dimensions: int
            dimensionSizes: int list
        } 
        with
            member __.Elements = __.elements
            member __.Dimensions = __.dimensions
            member __.DimensionSizes = __.dimensionSizes

open Domain 

[<RequireQualifiedAccess>]
module Tensor =
    let zeros<'T> (dims:int list) =
        {
            elements = Array.zeroCreate<'T> (dims |> List.reduce (fun i j -> i * j))
            dimensions = dims.Length
            dimensionSizes = dims
        }

    // (binary) operators 
    // indexing
    // broadcasting


