# FsTensor-Scratch

## Introduction
This repository is a playground to gain an understanding of how we would want to work with the concepts of Tensor<T> and Provider.
For more information please look at the summary of the discussions at the F# data science conference in Berlin '23 in the Wiki

https://github.com/fslaborg/Fs-Tensor-Scratch/wiki/FSharp-data-science-conference-'23-notes

## Setup explanation
After cloning the repository, run the following commands to setup the project:

- Use *dotnet tool restore*  for local tool restore.
    - nuget package manager: paket
    - deployment: fake
- Use *dotnet fake build* to build the project. The build artefacts will be located in the src/FsTensor/publish directory.
