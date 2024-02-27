# FIND_PLAYERS
A project to find football players in the footage of a football match broadcast.

The purpose of this work is to create a two-dimensional diagram of a football field with markers of football players marked on it, reflecting the real position of the players on the field.

This project was developed by me in the Unity development environment.

For implementation, I used two variations of the YOLO-8 neural network: for detecting players and for detecting key points of the field (central circle, penalty area).

Below is a frame from a real TV broadcast after preprocessing. Some morphological operations were applied to isolate the field and the detection of players using a neural network was performed.

<img src="https://github.com/zhernakov14/FIND_PLAYERS/assets/54941157/67bd5864-88b3-4c82-b9ae-54aa8fa3f8e6" width=50% height=50%> 

Of course, there are players that the neural network has not found, but there are very few such cases, such cases do not occur on every frame.

The following is the result of the algorithm - a two-dimensional diagram of the field with the players marked on it. This is achieved by converting the perspective of the frame and obtaining a straightened image.

<img src="https://github.com/zhernakov14/FIND_PLAYERS/assets/54941157/3c5b8dde-d6a2-4c95-8931-adeb88bc60dc" width=50% height=50%> 

Of course, this algorithm can be improved. For example, by adding a division of players into teams by determining the color of the area where the player was detected. Also, the use of neural networks will allow you to more accurately identify the field area.
