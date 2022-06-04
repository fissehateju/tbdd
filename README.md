TBDD (Territory-based Data Dissemination for Mobile Sink Wireless Sensor Networks) Simulator
This simulator is developed to evaluate TBDD. It has a graphical user interface that provides details of simulations execution and other pieces of information.

Developed by Fisseha Teju Wedaj et al. (fishteju@mail.ustc.edu.cn)

Implementation
The main windows graphical user interface (GUI) designs and implementations can be found at: https://github.com/fissehateju/tbdd/tree/main/ui. The Cells, spanning tree and Regions constructions are the backbones of this protocol, and they are located at: https://github.com/fissehateju/tbdd/tree/main/Constructor. Sensor nodes are given roles depending on their location. The role designation implemented at: https://github.com/fissehateju/tbdd/tree/main/Models/Cell. The mobility model for the mobile sink can be found at: https://github.com/fissehateju/tbdd/tree/main/Models/MobileModel. Finally, the data routing illustration implemented at: https://github.com/fissehateju/tbdd/tree/main/Dataplane, while Region's information broadcasting is implemented at: https://github.com/fissehateju/tbdd/blob/main/Region/ActiveRegBeacon.cs

How to run the toolkit
1- Open TBDD.csproj or TBDD.sln in the Toolkit folder. If the toolkit can¡¯t run pleases refer to http://staff.ustc.edu.cn/~anmande/miniflow/#_Installation_Problems.

2- Click start to run the program.

3- Import the required network topology, from File-> Import Topology.

3- Set the experiment settings, after clicking Experiment, and click Start.

4- After the scenario is run, results are obtained from Show Results ->