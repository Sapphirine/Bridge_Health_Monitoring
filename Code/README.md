#Bridge-Health-Monitoring: Codes
A simple description of the code files: see the project report for more information.

## Database generation codes

### opnBrIMBridgeModel.paramML
Parametric bridge model.

### Connection.cs
Connection between OpenBrIM and Larsa.

### larsaAnalysisEngineResultExtration.vb
Main simulation file: Loads the finite element model, runs the analysis on that model, then extracts the simulation results to xml format. 

### Lrs.cs
Larsa classes for communicating with OpenBrIM,

### LrsPlg.cs
Larsa user interface that connects Larsa to OpenBrIM.

## Simulation analysis codes

### Main.m
The main post-simulation analysis file. Computes similarity between acceleration output of simulations.

### my_fftDataMag
Computes the fft of a given dataset.

### sumpeaks.m
Computes the fft and sums the peaks of a given acceleration array.

### fftpeaks.m
Function to computes the peaks between a constant frequency wave.


