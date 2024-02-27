# FIND_PLAYERS
A project to find football players in the footage of a football match broadcast.

The purpose of this work is to create a two-dimensional diagram of a football field with markers of football players marked on it, reflecting the real position of the players on the field.

This project was developed by me in the Unity development environment.

For implementation, I used two variations of the YOLO-8 neural network: for detecting players and for detecting key points of the field (central circle, penalty area).

Below is a frame from a real TV broadcast after preprocessing. Some morphological operations were applied to isolate the field and the detection of players using a neural network was performed.ъ

![frame0](https://github.com/zhernakov14/FIND_PLAYERS/assets/54941157/67bd5864-88b3-4c82-b9ae-54aa8fa3f8e6)
