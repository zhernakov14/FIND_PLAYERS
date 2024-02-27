using System;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.VideoioModule;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.UnityUtils;
using UnityEngine.UI;
using OpenCVForUnity.ImgprocModule;
using System.Linq;
using Unity.Barracuda;
using UnityEngine.UIElements;
using OpenCVForUnity.VideoModule;
using Unity.Mathematics; 
using OpenCVForUnity.Xfeatures2dModule;
using Unity.VisualScripting;
using System.Security.Permissions;
using System.Runtime.InteropServices;


public class YOLO_test : MonoBehaviour
{
    public Texture2D textureImgScreen;
    public RawImage _rawImage;
    public NNModel kerasModelDetection, kerasModelSegmentation, kerasModelDetectionCircles;
    public GameObject _player;
    private Model myModelDetection, myModelSegmentation, myModelDetectionCircles;
    private IWorker workerDetection, workerSegmentation, workerDetectionCircles;
    private Texture2D textureImg, textureSourceImg, textureFrame, textureResult, textureField;
    private string outputLayer;
    //Mat img = Imgcodecs.imread("D:/UnityProjects/TEST/Assets/Sprites/11field.jpg");
    static int widthImg, heightImg, numberFrames;
    Mat img = Imgcodecs.imread("D:/UnityProjects/TEST/Assets/Sprites/11field.jpg");
    List<Texture2D> texturesFrames = new List<Texture2D>();
    List<int> historyOfParts = new List<int>(), listAddRight = new List<int>(), listAddLeft = new List<int>();
    private List<List<Point>> players = new List<List<Point>>(), centres = new List<List<Point>>(), centresAfterTransform = new List<List<Point>>(),
    pitchCentres18yardLeft = new List<List<Point>>(), pitchCentres18yardRight = new List<List<Point>>(), pitchCentres = new List<List<Point>>();
    int counter = 0, counterFrame = 0, numberVideo = 3, isCentre = 0, counterFrame18yardLeft = 0, counterFrame18yardRight = 0, counterAddLeft = 0, counterAddRight = 0,
    angle, partOfPitch, whatSee; // partOfPitch = 0 - левая половина, 1 - центр, 2 - правая половина
    Mat matrixPerspectiveTransform = new Mat(), mask = new Mat(), maskForPlayers = new Mat();
    Point left = new Point(), up = new Point(), right = new Point();
    bool centreFound = true, needKfY = false, isCentreForPers, isRightHalf, isLeftHalf;
    void Awake()
    {
        myModelDetection = ModelLoader.Load(kerasModelDetection);
        workerDetection = WorkerFactory.CreateWorker(myModelDetection, WorkerFactory.Device.GPU);

        myModelDetectionCircles = ModelLoader.Load(kerasModelDetectionCircles);
        workerDetectionCircles = WorkerFactory.CreateWorker(myModelDetectionCircles, WorkerFactory.Device.GPU);

        myModelSegmentation = ModelLoader.Load(kerasModelSegmentation);
        workerSegmentation = WorkerFactory.CreateWorker(myModelSegmentation, WorkerFactory.Device.GPU);

        Mat imgField = Imgcodecs.imread("D:/UnityProjects/TEST/Assets/Sprites/11field.jpg");
        textureField = new Texture2D(imgField.width(), imgField.height(), TextureFormat.RGBA32, false);
        Utils.matToTexture2D(imgField, textureField, true, 0, true, false, false);
        //sizeCanvas = _canvas.GetComponent<RectTransform>().sizeDelta;  
        _rawImage.texture = textureField;

        // ------------------------------------------------------------------------- блок для теста одного кадра
        // Mat currentFrame = img;
        // Imgproc.resize(currentFrame, currentFrame, new Size(1280, 1280));
        // widthImg = currentFrame.width();
        // heightImg = currentFrame.height();
        // Mat forDrawCircles = currentFrame.clone();
        
        // currentFrame = FindBorders(currentFrame); 
        // Imgcodecs.imwrite("D:/UnityProjects/TEST/Assets/Sprites/FindBorders.jpg", currentFrame);
        // currentFrame = TransformPerspective(currentFrame);  
        // Imgcodecs.imwrite("D:/UnityProjects/TEST/Assets/Sprites/TransformPerspective.jpg", currentFrame);
        // DrawCircles(forDrawCircles); 

        // textureFrame = new Texture2D(widthImg, heightImg, TextureFormat.RGBA32, false);
        // Utils.matToTexture2D(currentFrame, textureFrame, true, 0, true, false, false);
        // texturesFrames.Add(textureFrame);

        // Instantiate(_player, new Vector3(0, 0, 0), Quaternion.Euler(0, 0, 0));
        // Instantiate(_player, new Vector3(1280/2, -830/2, 0), Quaternion.Euler(0, 0, 0));
        // -------------------------------------------------------------------------

        VideoCapture capture = new VideoCapture("D:/UnityProjects/TEST/Assets/Video/"+ numberVideo.ToString() +"/" + numberVideo.ToString() + ".mp4");

        Time.fixedDeltaTime = 1f; // 1 / Time.fixedDeltaTime это количество вызовов FixedUpdate() в секунду
        int delay = 10;
        numberFrames = (int)(capture.get(Videoio.CAP_PROP_FRAME_COUNT) / 10);
        Debug.Log(numberFrames);
        Debug.Log(capture.get(Videoio.CAP_PROP_FRAME_HEIGHT));
        Debug.Log(capture.get(Videoio.CAP_PROP_FRAME_WIDTH));
        Debug.Log(capture.get(Videoio.CAP_PROP_FPS));

        Mat currentFrame = new Mat();
        

        while (capture.get(Videoio.CAP_PROP_POS_FRAMES) <= (numberFrames-1) * 10)
        { 
            capture.read(currentFrame); // читаем кадр
            if ((capture.get(Videoio.CAP_PROP_POS_FRAMES) - 1) % delay == 0)
            { 
                Imgproc.resize(currentFrame, currentFrame, new Size(1280, 1280));
                widthImg = currentFrame.width();
                heightImg = currentFrame.height();
                Mat forDrawCircles = currentFrame.clone();
                Imgcodecs.imwrite(String.Format("Assets/Video/"+ numberVideo.ToString() +"/afterTransformPerspective2/frame{0}.jpg", counterFrame), currentFrame); // записываем кадр в файл
                
                currentFrame = FindBorders(currentFrame); // ищем поле
                mask = currentFrame;
                Debug.Log("currentFrame " + currentFrame.cols() + " " + "currentFrame " + currentFrame.rows());
                Imgcodecs.imwrite(String.Format("Assets/Video/"+ numberVideo.ToString() +"/afterFindBorders/frame{0}.jpg", counterFrame), currentFrame); // записываем кадр в файл
                currentFrame = TransformPerspective(currentFrame); // выправляем перспективу
                Debug.Log("currentFrame " + currentFrame.cols() + " " + "currentFrame " + currentFrame.rows());
                Imgcodecs.imwrite(String.Format("Assets/Video/"+ numberVideo.ToString() +"/afterTransformPerspective/frame{0}.jpg", counterFrame), currentFrame); // записываем кадр в файл
                DrawCircles(forDrawCircles); //заполняем листы координат игроков и центра поля (или 18yard)
                CalculateNewCoords(players, centres, centresAfterTransform, matrixPerspectiveTransform, currentFrame); //пересчет координат игроков и точек поля с помощью матрицы преобразования
                CalculateCoords2DScheme(players, centresAfterTransform);

                Debug.Log("centres " + centres.Count);
                Debug.Log("counterFrame " + counterFrame);
                Debug.Log("widthImg" + widthImg + " " + "heightImg" + heightImg);
                Debug.Log("currentFrame " + currentFrame.cols() + " " + "currentFrame " + currentFrame.rows());
                textureFrame = new Texture2D(widthImg, heightImg, TextureFormat.RGBA32, false);
                Utils.matToTexture2D(currentFrame, textureFrame, true, 0, true, false, false);
                texturesFrames.Add(textureFrame);
                Debug.Log("currentFrame aedrgjerigjer " + players.Count());
                
                //Imgcodecs.imwrite(String.Format("Assets/Video/1/frame{0}.jpg", counterFrame), currentFrame); // записываем кадр в файл
                counterFrame++;
                if (currentFrame.empty())
                {
                    Debug.Log("frame is empty");
                    break;
                }
            } 
        }  
    }

    void FixedUpdate()
    {
        //ScreenCapture.CaptureScreenshot("screen_" + counterFrame.ToString()+ ".png");
        //ScreenCapture.CaptureScreenshot("Assets/Video/"+ numberVideo.ToString() +"/result/screenshot " + System.DateTime.Now.ToString("MM-dd-yy (HH-mm-ss)") + ".png");
        if (counter < counterFrame)
        {
            for (int i = 0; i < players[counter].Count(); i++)
            {
                int newX = (int)players[counter][i].x;
                int newY = (int)players[counter][i].y;
                GameObject player = Instantiate(_player, new Vector3(newX, -newY, 0), Quaternion.Euler(0, 0, 0));
                Destroy(player, Time.fixedDeltaTime);
            }

            counter++;
        }

        // if (counter < texturesFrames.Count)
        // {
        //     _rawImage.texture = texturesFrames[counter];
        //     counter++;
        // }
        
    }

    void DetectPlayers(Mat inputImg)
    {
        Imgproc.morphologyEx(maskForPlayers, maskForPlayers, Imgproc.MORPH_DILATE, Imgproc.getStructuringElement(Imgproc.MORPH_ELLIPSE, new Size(100, 100)));
        Mat outImg = new Mat(inputImg.rows(), inputImg.cols(), CvType.CV_8UC3, new Scalar(0, 0, 0));
        Core.copyTo(inputImg, outImg, maskForPlayers);
        Imgcodecs.imwrite(String.Format("Assets/Video/"+ numberVideo.ToString() +"/afterMaskPlayers/frame{0}.jpg", counterFrame), outImg);

        int channelCount = 3; //grayscale, 3 = color, 4 = color+alpha
        textureSourceImg = new Texture2D(inputImg.width(), inputImg.height(), TextureFormat.RGBA32, false);
        Utils.matToTexture2D(outImg, textureSourceImg, true, 0, true, false, false);
        Tensor inputXPlayers = new Tensor(textureSourceImg, channelCount);
        //Debug.Log("SHAPE" + inputXPlayers.shape);

        // Peek at the output tensor without copying it.
        Tensor outputYPlayers = workerDetection.Execute(inputXPlayers).PeekOutput();
        //Debug.Log("SHAPEEE" + outputYPlayers.shape);
        float probPlayer;
        bool addFlag = true;
        List<Point> centresPlayers = new List<Point>(), centresPlayersResult = new List<Point>();;
        List<int> heightsRect = new List<int>();
        for (int i = 0; i < 33600; i++)
        {
            probPlayer = outputYPlayers[0, 0, i, 7]; // 7 - это игрок
            // if (probRef > 0.5)
            // {
            //     Point centreRef = new((int)outputY[0, 0, i, 0], (int)outputY[0, 0, i, 1]); 
            //     Imgproc.circle(resultImg, centreRef, 3, new Scalar(0, 0, 255), 5);
            // }
            if (probPlayer > 0.4)
            {
                Point centre = new((int)outputYPlayers[0, 0, i, 0], (int)outputYPlayers[0, 0, i, 1]); 
                for (int n = 0; n < centresPlayers.Count; n++) 
                {
                    if ((Math.Abs(centresPlayers[n].x - centre.x) < 30) && (Math.Abs(centresPlayers[n].y - centre.y) < 30))
                    {
                        addFlag = false;
                        break;
                    }
                    else
                    {
                        addFlag = true;
                    }
                }
                if (addFlag == true)
                {
                    Imgproc.circle(inputImg, centre, 10, new Scalar(255, 0, 0), 5);
                    Imgcodecs.imwrite(String.Format("Assets/Video/"+ numberVideo.ToString() +"/afterMaskCircles/frame{0}.jpg", counterFrame), inputImg);
                    centresPlayers.Add(centre);  
                    if (partOfPitch == 0 || partOfPitch == 2) 
                        centresPlayersResult.Add(new Point(centre.x + listAddLeft[counterAddLeft-1], centre.y + (int)outputYPlayers[0, 0, i, 3]/2));                    
                    else
                        centresPlayersResult.Add(new Point(centre.x, centre.y + (int)outputYPlayers[0, 0, i, 3]/2));        
                }  
            }
        }
        Debug.Log("centresPlayers.Count " + centresPlayers.Count);
        players.Add(centresPlayersResult);
        inputXPlayers.Dispose();
        //return centresPlayers;
    }

    void DetectCircles(Mat inputImg) // метод для заполнения листа pitchCentres
    {
        //Core.bitwise_and(inputImg, mask, inputImg);
        Mat outImg = new Mat(inputImg.rows(), inputImg.cols(), CvType.CV_8UC3, new Scalar(0, 0, 0));
        Core.copyTo(inputImg, outImg, mask);
        Imgcodecs.imwrite(String.Format("Assets/Video/"+ numberVideo.ToString() +"/afterMaskCircles/frame{0}.jpg", counterFrame), outImg);

        int channelCount = 3; //grayscale, 3 = color, 4 = color+alpha
        textureSourceImg = new Texture2D(inputImg.width(), inputImg.height(), TextureFormat.RGBA32, false);
        Utils.matToTexture2D(outImg, textureSourceImg, true, 0, true, false, false);
        Tensor inputXCircles = new Tensor(textureSourceImg, channelCount);
        //Debug.Log("SHAPE" + inputXCircles.shape);

        // Peek at the output tensor without copying it.
        Tensor outputYCircles = workerDetectionCircles.Execute(inputXCircles).PeekOutput();
        //Debug.Log("SHAPEEE" + outputYCircles.shape);
        List<float> probLeftHalfCircles = new List<float>(), probRightHalfCircles = new List<float>(), prob18Yard = new List<float>();
        List<Point> threePointOfCentre = new List<Point>(), threePointOfCentreGlobal = new List<Point>(3); // лист для координат трех точек найденного центра
        Point pitchCentre = new Point();
        Mat experiment = outImg.clone();

        for (int i = 0; i < 33600; i++)
        {
            // names: ['18Yard', '18Yard Circle', '5Yard', 'First Half Central Circle', 'First Half Field', 'Second Half Central Circle', 'Second Half Field']
            if (partOfPitch == 1)
            {
                probLeftHalfCircles.Add(outputYCircles[0, 0, i, 7]);
                probRightHalfCircles.Add(outputYCircles[0, 0, i, 9]);
            }
            else
            {
                prob18Yard.Add(outputYCircles[0, 0, i, 5]);
            }
        }
        //Debug.Log("probLeftHalfCircles.Max() " + probLeftHalfCircles.Max());
        // var ratioLeft = 1.0 / probLeftHalfCircles.Max();
        // var normalizedListLeft = probLeftHalfCircles.Select(i => i * ratioLeft).ToList();

        // var ratioRight = 1.0 / probRightHalfCircles.Max();
        // var normalizedListRight = probRightHalfCircles.Select(i => i * ratioRight).ToList();

        // var ratioLeftHalf = 1.0 / probLeftHalf.Max();
        // var normalizedListLeftHalf = probLeftHalf.Select(i => i * ratioLeftHalf).ToList();

        // var ratioRightHalf = 1.0 / probRightHalf.Max();
        // var normalizedListRightHalf = probRightHalf.Select(i => i * ratioRightHalf).ToList();

        if (partOfPitch == 1)
        {
            List<float> sortProbLeftHalfCircles = new List<float>(probLeftHalfCircles);
            sortProbLeftHalfCircles.Sort();
            int indexMaxLeft = probLeftHalfCircles.IndexOf(sortProbLeftHalfCircles.Max());
            //Debug.Log("probLeftHalfCircles.Max() " + probLeftHalfCircles.Max());
            Point centreLeft = new((int)outputYCircles[0, 0, indexMaxLeft, 0], (int)outputYCircles[0, 0, indexMaxLeft, 1]);  
            
            List<float> sortProbRightHalfCircles = new List<float>(probRightHalfCircles);
            sortProbRightHalfCircles.Sort();
            int indexMaxRight = probRightHalfCircles.IndexOf(sortProbRightHalfCircles[33599]);
            //Debug.Log("probRightHalfCircles.Max() " + probRightHalfCircles.Max());
            Point centreRight = new((int)outputYCircles[0, 0, indexMaxRight, 0], (int)outputYCircles[0, 0, indexMaxRight, 1]); 

            bool centreIsDraw = false, flag = true;
            int maxWidthRight = 0, maxWidthLeft = 0, maxHeightUp = 0, maxHeightDown = 0;
            while (centreIsDraw == false && flag == true)
            {
                for (int r = 0; flag && r < 20; r++)
                {
                    for (int l = 0; flag && l < 20; l++)
                    {
                        indexMaxRight = probRightHalfCircles.IndexOf(sortProbRightHalfCircles[33599 - r]);
                        centreRight.x = (int)outputYCircles[0, 0, indexMaxRight, 0];
                        centreRight.y = (int)outputYCircles[0, 0, indexMaxRight, 1];

                        indexMaxLeft = probLeftHalfCircles.IndexOf(sortProbLeftHalfCircles[33599 - l]);
                        centreLeft.x = (int)outputYCircles[0, 0, indexMaxLeft, 0];
                        centreLeft.y = (int)outputYCircles[0, 0, indexMaxLeft, 1];

                        Point pt1 = new Point(centreLeft.x - (int)outputYCircles[0, 0, indexMaxLeft, 2]/2, centreLeft.y - (int)outputYCircles[0, 0, indexMaxLeft, 3]/2);
                        Point pt2 = new Point(centreLeft.x + (int)outputYCircles[0, 0, indexMaxLeft, 2]/2, centreLeft.y + (int)outputYCircles[0, 0, indexMaxLeft, 3]/2);
                        Point pt3 = new Point(centreRight.x - (int)outputYCircles[0, 0, indexMaxRight, 2]/2, centreRight.y - (int)outputYCircles[0, 0, indexMaxRight, 3]/2);
                        Point pt4 = new Point(centreRight.x + (int)outputYCircles[0, 0, indexMaxRight, 2]/2, centreRight.y + (int)outputYCircles[0, 0, indexMaxRight, 3]/2);

                        if ((Math.Abs(centreLeft.x - centreRight.x) >= 50) && (Math.Abs(centreLeft.x - centreRight.x) <= 640) && 
                            (Math.Abs(centreLeft.y - centreRight.y) <= 100) && (pt2.y - pt1.y) - (pt4.y - pt3.y) <= 100)
                        {
                            Debug.Log("centreLeft.x = " + centreLeft.x + " centreLeft.y = " + centreLeft.y);
                            Debug.Log("centreRight.x = " + centreRight.x + " centreRight.y = " + centreRight.y);
                            if (centreLeft.x < centreRight.x)
                            {
                                if (centreLeft.x > pt3.x && centreLeft.x < pt4.x)
                                {
                                    Debug.Log("werwerwer12345");
                                    pitchCentre = new Point(pt4.x, (pt3.y + pt4.y)/2);
                                    flag = false;
                                    maxWidthRight = -(int)(pitchCentre.x - pt3.x);
                                    if (pt1.y <= pt3.y)
                                        maxHeightUp = -(int)(pitchCentre.y - pt1.y);
                                    else
                                        maxHeightUp = -(int)(pitchCentre.y - pt3.y);
                                    if (pt2.y <= pt4.y)
                                        maxHeightDown = -(int)(pitchCentre.y - pt4.y);
                                    else
                                        maxHeightDown = -(int)(pitchCentre.y - pt2.y);
                                }
                                else
                                {
                                    pitchCentre = new Point(centreLeft.x + (int)outputYCircles[0, 0, indexMaxLeft, 2]/2, centreLeft.y);
                                    flag = false;
                                    maxWidthRight = (int)(pt4.x - pitchCentre.x);
                                    maxWidthLeft = -(int)(pitchCentre.x - pt1.x);
                                    if (pt1.y <= pt3.y)
                                        maxHeightUp = -(int)(pitchCentre.y - pt1.y);
                                    else
                                        maxHeightUp = -(int)(pitchCentre.y - pt3.y);
                                    if (pt2.y <= pt4.y)
                                        maxHeightDown = -(int)(pitchCentre.y - pt4.y);
                                    else
                                        maxHeightDown = -(int)(pitchCentre.y - pt2.y);
                                }  
                            }
                            else
                            {
                                if (centreRight.x > pt1.x && centreRight.x < pt2.x)
                                {
                                    Debug.Log("werwerwer12345");
                                    pitchCentre = new Point(pt2.x, (pt1.y + pt2.y)/2);
                                    flag = false;
                                    maxWidthLeft = -(int)(pitchCentre.x - pt1.x);
                                    maxHeightUp = -(int)(pitchCentre.y - pt1.y);
                                    if (pt2.y <= pt4.y)
                                        maxHeightDown = -(int)(pitchCentre.y - pt4.y);
                                    else
                                        maxHeightDown = -(int)(pitchCentre.y - pt2.y);
                                }
                                else
                                {
                                    pitchCentre = new Point(centreLeft.x - (int)outputYCircles[0, 0, indexMaxLeft, 2]/2, centreLeft.y);
                                    maxWidthRight = (int)(pt2.x - pitchCentre.x);
                                    maxWidthLeft = -(int)(pitchCentre.x - pt3.x);
                                    if (pt1.y <= pt3.y)
                                        maxHeightUp = -(int)(pitchCentre.y - pt1.y);
                                    else
                                        maxHeightUp = -(int)(pitchCentre.y - pt3.y);
                                    if (pt2.y <= pt4.y)
                                        maxHeightDown = -(int)(pitchCentre.y - pt4.y);
                                    else
                                        maxHeightDown = -(int)(pitchCentre.y - pt2.y);
                                    flag = false;
                                }
                            }
                            centreIsDraw = true;

                            Point up = new Point(pitchCentre.x, pitchCentre.y + maxHeightUp); // верх
                            Point down = new Point(pitchCentre.x, pitchCentre.y + maxHeightDown); // низ
                            Point right = new Point(pitchCentre.x + maxWidthRight, pitchCentre.y); // право
                            Point left = new Point (pitchCentre.x + maxWidthLeft, pitchCentre.y); // лево

                            Imgproc.rectangle(experiment, pt1, pt2, new Scalar(255, 255, 0), 10);
                            Imgproc.rectangle(experiment, pt3, pt4, new Scalar(255, 255, 0), 10);

                            threePointOfCentre.Add(up);                
                            threePointOfCentre.Add(down);
                            
                            Imgproc.circle(experiment, up, 3, new Scalar(255, 0, 0), 10);
                            Imgproc.circle(experiment, down, 3, new Scalar(255, 0, 0), 10);

                            if (Math.Abs(maxWidthRight) > Math.Abs(maxWidthLeft))
                            {
                                Imgproc.circle(experiment, right, 3, new Scalar(255, 0, 0), 10);
                                threePointOfCentre.Add(right);                             
                                whatSee = 1;
                            }
                            else
                            {
                                Imgproc.circle(experiment, left, 3, new Scalar(255, 0, 0), 10);
                                threePointOfCentre.Add(left);
                                whatSee = 0;
                            }

                            Imgcodecs.imwrite(String.Format("Assets/Video/" + numberVideo.ToString() + "/experiment/frame{0}.jpg", counterFrame), experiment);
                        }
                    }
                }
                if (flag)
                {
                    indexMaxRight = probRightHalfCircles.IndexOf(sortProbRightHalfCircles[33599]);
                    centreRight.x = (int)outputYCircles[0, 0, indexMaxRight, 0];
                    centreRight.y = (int)outputYCircles[0, 0, indexMaxRight, 1];

                    indexMaxLeft = probLeftHalfCircles.IndexOf(sortProbLeftHalfCircles[33599]);
                    centreLeft.x = (int)outputYCircles[0, 0, indexMaxLeft, 0];
                    centreLeft.y = (int)outputYCircles[0, 0, indexMaxLeft, 1];

                    Point pt1 = new Point(centreLeft.x - (int)outputYCircles[0, 0, indexMaxLeft, 2]/2, centreLeft.y - (int)outputYCircles[0, 0, indexMaxLeft, 3]/2);
                    Point pt2 = new Point(centreLeft.x + (int)outputYCircles[0, 0, indexMaxLeft, 2]/2, centreLeft.y + (int)outputYCircles[0, 0, indexMaxLeft, 3]/2);
                    Point pt3 = new Point(centreRight.x - (int)outputYCircles[0, 0, indexMaxRight, 2]/2, centreRight.y - (int)outputYCircles[0, 0, indexMaxRight, 3]/2);
                    Point pt4 = new Point(centreRight.x + (int)outputYCircles[0, 0, indexMaxRight, 2]/2, centreRight.y + (int)outputYCircles[0, 0, indexMaxRight, 3]/2);

                    if (Math.Abs(centreRight.x - experiment.cols()) < centreRight.x)
                    {
                        pitchCentre = new Point(centreLeft.x + (int)outputYCircles[0, 0, indexMaxLeft, 2]/2, centreRight.y);
                        maxWidthLeft = -(int)(pitchCentre.x - pt3.x);    
                    }
                    else
                    {
                        pitchCentre = new Point(centreLeft.x - (int)outputYCircles[0, 0, indexMaxLeft, 2]/2, centreRight.y);
                        maxWidthRight = (int)(pt4.x - pitchCentre.x);
                    }
                    
                    Point up = new Point(pitchCentre.x, pt3.y); // верх
                    Point down = new Point(pitchCentre.x, pt4.y); // низ
                    Point right = new Point(pitchCentre.x + maxWidthRight, pitchCentre.y); // право
                    Point left = new Point (pitchCentre.x + maxWidthLeft, pitchCentre.y); // лево

                    Imgproc.rectangle(experiment, pt1, pt2, new Scalar(255, 255, 0), 10);
                    Imgproc.rectangle(experiment, pt3, pt4, new Scalar(255, 255, 0), 10);

                    threePointOfCentre.Add(up);
                    threePointOfCentre.Add(down);

                    Imgproc.circle(experiment, up, 3, new Scalar(255, 0, 0), 10);
                    Imgproc.circle(experiment, down, 3, new Scalar(255, 0, 0), 10);

                    if (Math.Abs(maxWidthRight) > Math.Abs(maxWidthLeft))
                    {
                        Imgproc.circle(experiment, right, 3, new Scalar(255, 0, 0), 10);
                        threePointOfCentre.Add(right);                             
                        whatSee = 1;
                    }
                    else
                    {
                        Imgproc.circle(experiment, left, 3, new Scalar(255, 0, 0), 10);
                        threePointOfCentre.Add(left);
                        whatSee = 0;
                    }

                    Imgcodecs.imwrite(String.Format("Assets/Video/" + numberVideo.ToString() + "/experiment/frame{0}.jpg", counterFrame), experiment);
                    flag = false;
                }
    //-------------------------------------------------------------------------------------------------------------------------------------------------------

                // if ((Math.Abs(centreLeft.x - centreRight.x) >= 50) && (Math.Abs(centreLeft.x - centreRight.x) <= 640) && 
                //     (Math.Abs(centreLeft.y - centreRight.y) <= 100) && (pt2.y - pt1.y) - (pt4.y - pt3.y) <= 100)
                // {
                //     // if ((int)outputYCircles[0, 0, indexMaxLeft, 2] > (int)outputYCircles[0, 0, indexMaxRight, 2])
                //     // {
                //     if (centreLeft.x < centreRight.x)
                //     {
                //         if (centreLeft.x > pt3.x && centreLeft.x < pt4.x)
                //         {
                //             pitchCentre = new Point(pt4.x, (pt3.y + pt4.y)/2);
                //         }
                //         else
                //         {
                //             pitchCentre = new Point(centreLeft.x + (int)outputYCircles[0, 0, indexMaxLeft, 2]/2, centreLeft.y);
                //         }  
                //     }
                //     else
                //     {
                //         pitchCentre = new Point(centreLeft.x - (int)outputYCircles[0, 0, indexMaxLeft, 2]/2, centreLeft.y);
                //     }
                //     //}
                //     //pitchCentre = new Point(centreLeft.x, centreLeft.y);
                //     //pitchCentre = new Point(centreLeft.x + (int)outputYCircles[0, 0, indexMaxLeft, 2]/2, centreLeft.y);
                //     centreIsDraw = true;
                //     Imgproc.rectangle(experiment, pt1, pt2, new Scalar(255, 255, 0), 10);
                //     Imgproc.rectangle(experiment, pt3, pt4, new Scalar(255, 255, 0), 10);
                //     Imgcodecs.imwrite(String.Format("Assets/Video/" + numberVideo.ToString() + "/experiment/frame{0}.jpg", counterFrame), experiment);
                //     // centresCircles.Add(centreLeft);
                //     // centresCircles.Add(centreRight);
                // }
                // else
                // {
                //     // pitchCentre = new Point(centreLeft.x , centreLeft.y );
                //     // centreIsDraw = true;
                //     if (iteration == 6)
                //     {
                //         pitchCentre = new Point(centreLeft.x, centreLeft.y);
                //         Imgproc.rectangle(experiment, pt1, pt2, new Scalar(255, 255, 0), 10);
                //         Imgproc.rectangle(experiment, pt3, pt4, new Scalar(255, 255, 0), 10);
                //         Imgcodecs.imwrite(String.Format("Assets/Video/" + numberVideo.ToString() + "/experiment/frame{0}.jpg", counterFrame), experiment);
                //     }
                //     Debug.Log("ooooooooooooooooooooooooooooooooo");
                //     iteration++;
                //     // indexMaxLeft = probLeftHalfCircles.IndexOf(sortProbLeftHalfCircles[33599 - iteration]);
                //     // centreLeft.x = (int)outputYCircles[0, 0, indexMaxLeft, 0];
                //     // centreLeft.y = (int)outputYCircles[0, 0, indexMaxLeft, 1];
                // }
            }
            pitchCentres.Add(threePointOfCentre);

            centres.Add(threePointOfCentre);

            inputXCircles.Dispose();
            // return pitchCentre;
        }
        else if (partOfPitch == 0)
        {
            whatSee = 2; // видим левую штрафную
            bool isAdd = false;

            List<float> sortProbCircle18yard = new List<float>(prob18Yard);
            sortProbCircle18yard.Sort();
            int indexMax18yard = prob18Yard.IndexOf(sortProbCircle18yard.Max());
            //Debug.Log("probLeftHalfCircles.Max() " + probLeftHalfCircles.Max());
            Point centre18yard = new((int)outputYCircles[0, 0, indexMax18yard, 0], (int)outputYCircles[0, 0, indexMax18yard, 1]);  
            while(centre18yard.x > widthImg*5/6)
            {
                indexMax18yard -= 1;
                centre18yard = new((int)outputYCircles[0, 0, indexMax18yard, 0], (int)outputYCircles[0, 0, indexMax18yard, 1]);  
            }
             
            Point pt1 = new Point(centre18yard.x - (int)outputYCircles[0, 0, indexMax18yard, 2]/2, centre18yard.y - (int)outputYCircles[0, 0, indexMax18yard, 3]/2);
            Point pt2 = new Point(centre18yard.x + (int)outputYCircles[0, 0, indexMax18yard, 2]/2, centre18yard.y + (int)outputYCircles[0, 0, indexMax18yard, 3]/2);
            Imgproc.rectangle(experiment, pt1, pt2, new Scalar(255, 255, 0), 10);

            Point rightUp = new Point(), centr = new Point(), leftDown = new Point();

            if (counterFrame18yardLeft != 0 && counterFrame18yardLeft <= pitchCentres18yardLeft.Count)
            {
                if(Math.Abs(centre18yard.x - pitchCentres18yardLeft[counterFrame18yardLeft-1][1].x) > widthImg/3 && Math.Abs(pt2.x - widthImg) > 20)
                {
                    rightUp = pitchCentres18yardLeft[counterFrame18yardLeft-1][0];
                    centr = pitchCentres18yardLeft[counterFrame18yardLeft-1][1];
                    leftDown = pitchCentres18yardLeft[counterFrame18yardLeft-1][2];
                    isAdd = true;
                } 
            }
            if (isAdd == false)
            {
                if (Math.Abs(pt2.x - widthImg) < 20 && pitchCentres18yardLeft[counterFrame18yardLeft-1][1].x < widthImg/2) // проверка на резкий переброс точки на другой конец кадра
                {
                    rightUp = new Point(pt2.x, pt1.y);
                    centr = new Point(pt2.x, centre18yard.y);
                    leftDown = new Point(pt2.x, pt2.y);
                }
                else if (Math.Abs(pt1.x) > 20) // немного передвигаем центральную и правую верхнюю точки для достоверности
                {
                    rightUp = new Point(pt2.x - 40, pt1.y);
                    centr = new Point(pt2.x - 20, centre18yard.y);
                    leftDown = new Point(pt1.x, pt2.y);
                }
                else
                {
                    rightUp = new Point(pt2.x, pt1.y);
                    centr = new Point(pt2.x, centre18yard.y);
                    leftDown = new Point(pt1.x, pt2.y);
                }
            }

            threePointOfCentre.Add(rightUp);
            threePointOfCentre.Add(leftDown);
            threePointOfCentre.Add(centr);

            pitchCentres18yardLeft.Add(threePointOfCentre);

            threePointOfCentreGlobal.Add(new Point(threePointOfCentre[0].x + listAddLeft[counterAddLeft-1], threePointOfCentre[0].y));
            threePointOfCentreGlobal.Add(new Point(threePointOfCentre[1].x + listAddLeft[counterAddLeft-1], threePointOfCentre[1].y));
            threePointOfCentreGlobal.Add(new Point(threePointOfCentre[2].x + listAddLeft[counterAddLeft-1], threePointOfCentre[2].y));

            Imgproc.circle(experiment, threePointOfCentre[0], 3, new Scalar(255, 255, 255), 10);
            Imgproc.circle(experiment, threePointOfCentre[1], 3, new Scalar(255, 255, 255), 10);
            Imgproc.circle(experiment, threePointOfCentre[2], 3, new Scalar(255, 255, 255), 10);

            Imgcodecs.imwrite(String.Format("Assets/Video/" + numberVideo.ToString() + "/experiment/frame{0}.jpg", counterFrame), experiment);       

            centres.Add(threePointOfCentreGlobal);
            counterFrame18yardLeft ++;
            inputXCircles.Dispose();
            // return centre18yard;
        }
        else 
        {
            whatSee = 3; // видим правую штрафную
            bool isAdd = false;

            List<float> sortProbCircle18yard = new List<float>(prob18Yard);
            sortProbCircle18yard.Sort();
            int indexMax18yard = prob18Yard.IndexOf(sortProbCircle18yard.Max());
            //Debug.Log("probLeftHalfCircles.Max() " + probLeftHalfCircles.Max());
            Point centre18yard = new((int)outputYCircles[0, 0, indexMax18yard, 0], (int)outputYCircles[0, 0, indexMax18yard, 1]);  
            while(centre18yard.x < widthImg/6)
            {
                indexMax18yard -= 1;
                centre18yard = new((int)outputYCircles[0, 0, indexMax18yard, 0], (int)outputYCircles[0, 0, indexMax18yard, 1]);  
            }

            Point pt1 = new Point(centre18yard.x - (int)outputYCircles[0, 0, indexMax18yard, 2]/2, centre18yard.y - (int)outputYCircles[0, 0, indexMax18yard, 3]/2);
            Point pt2 = new Point(centre18yard.x + (int)outputYCircles[0, 0, indexMax18yard, 2]/2, centre18yard.y + (int)outputYCircles[0, 0, indexMax18yard, 3]/2);
            Imgproc.rectangle(experiment, pt1, pt2, new Scalar(255, 255, 0), 10);

            Point leftUp = new Point(), centr = new Point(), rightDown = new Point();

            if (counterFrame18yardRight != 0 && counterFrame18yardRight <= pitchCentres18yardRight.Count)
            {
                if(Math.Abs(centre18yard.x - pitchCentres18yardRight[counterFrame18yardRight-1][1].x) > widthImg/3 && pt1.x > 20)
                {
                    leftUp = pitchCentres18yardRight[counterFrame18yardRight-1][0];
                    centr = pitchCentres18yardRight[counterFrame18yardRight-1][1];
                    rightDown = pitchCentres18yardRight[counterFrame18yardRight-1][2];
                    isAdd = true;
                } 
            }
            if (isAdd == false)
            {
                Debug.Log("counterFrame18yardRight " + counterFrame18yardRight);
                if (counterFrame18yardRight != 0 && pt1.x < 20 && pitchCentres18yardRight[counterFrame18yardRight-1][1].x > widthImg/2)
                {
                    leftUp = new Point(pt2.x, pt1.y);
                    centr = new Point(pt2.x, centre18yard.y);
                    rightDown = new Point(pt2.x, pt2.y);
                }
                else if (Math.Abs(pt2.x - widthImg) > 20) // немного передвигаем центральную и левую верхнюю точки для достоверности
                {
                    leftUp = new Point(pt1.x + 40, pt1.y);;
                    centr = new Point(pt1.x + 20, centre18yard.y);
                    rightDown = pt2;
                }
                else
                {
                    leftUp = pt1;
                    centr = new Point(pt1.x, centre18yard.y);
                    rightDown = pt2;
                }
            }

            threePointOfCentre.Add(leftUp);
            threePointOfCentre.Add(rightDown);
            threePointOfCentre.Add(centr);
            
            pitchCentres18yardRight.Add(threePointOfCentre);

            threePointOfCentreGlobal.Add(new Point(threePointOfCentre[0].x + listAddLeft[counterAddLeft-1], threePointOfCentre[0].y));
            threePointOfCentreGlobal.Add(new Point(threePointOfCentre[1].x + listAddLeft[counterAddLeft-1], threePointOfCentre[1].y));
            threePointOfCentreGlobal.Add(new Point(threePointOfCentre[2].x + listAddLeft[counterAddLeft-1], threePointOfCentre[2].y));

            Imgproc.circle(experiment, threePointOfCentre[0], 3, new Scalar(255, 255, 255), 10);
            Imgproc.circle(experiment, threePointOfCentre[1], 3, new Scalar(255, 255, 255), 10);
            Imgproc.circle(experiment, threePointOfCentre[2], 3, new Scalar(255, 255, 255), 10);

            Imgcodecs.imwrite(String.Format("Assets/Video/" + numberVideo.ToString() + "/experiment/frame{0}.jpg", counterFrame), experiment);

            centres.Add(threePointOfCentreGlobal);
            counterFrame18yardRight ++;
            inputXCircles.Dispose();
            // return centre18yard;
        }
        // if ((Math.Abs(centreLeft.x - centreRight.x) >= 50) || (Math.Abs(centreLeft.y - centreRight.y) >= 50))
        // {
        //     pitchCentre = new Point((centreLeft.x + centreRight.x) / 2, (centreLeft.y + centreRight.y) / 2);
        //     // centresCircles.Add(centreLeft);
        //     // centresCircles.Add(centreRight);
        // }
        // else
        // {
        //     if ((int)outputYCircles[0, 0, indexMaxLeft, 2] > (int)outputYCircles[0, 0, indexMaxRight, 2])
        //     {
        //         centresCircles.Add(centreLeft);
        //         widthRectCircle = (int)outputYCircles[0, 0, indexMaxLeft, 2];
        //     }
        //     else
        //     {
        //         Debug.Log("ooooooooooooooooooooooooooooooooo");
        //         centresCircles.Add(centreRight);
        //         widthRectCircle = -(int)outputYCircles[0, 0, indexMaxRight, 2];
        //     }
        // }

        // int indexMaxLeftHalf = probLeftHalf.IndexOf(probLeftHalf.Max());
        // Debug.Log("probLeftHalfCircles.Max() " + probLeftHalf.Max());
        // Point centreLeftHalf = new((int)outputYCircles[0, 0, indexMaxLeftHalf, 0], (int)outputYCircles[0, 0, indexMaxLeftHalf, 1]);  
        // centresCircles.Add(centreLeftHalf);

        // int indexMaxRightHalf = probRightHalf.IndexOf(probRightHalf.Max());
        // Debug.Log("probRightHalfCircles.Max() " + probRightHalf.Max());
        // Point centreRightHalf = new((int)outputYCircles[0, 0, indexMaxRightHalf, 0], (int)outputYCircles[0, 0, indexMaxRightHalf, 1]);  
        // centresCircles.Add(centreRightHalf);

        // if (normalizedListLeft.Max() > normalizedListRight.Max())
        // {
        //     Point centre = new((int)outputYCircles[0, 0, indexMaxLeft, 0], (int)outputYCircles[0, 0, indexMaxLeft, 1]);  
        //     widthRectCircle = (int)outputYCircles[0, 0, indexMaxLeft, 2];
        //     centresCircles.Add(centre);
        // }
        // else
        // {
        //     Point centre = new((int)outputYCircles[0, 0, indexMaxRight, 0], (int)outputYCircles[0, 0, indexMaxRight, 1]);  
        //     widthRectCircle = -(int)outputYCircles[0, 0, indexMaxRight, 2];
        //     centresCircles.Add(centre);
        // }


        // int countLeft = 0;
        // int countRight = 0;

        // foreach (double prob in normalizedListLeft)
        // {
        //     if (prob > 0.7)
        //     {
        //         countLeft++;
        //     }
        // }
        // foreach (double prob in normalizedListRight)
        // {
        //     if (prob > 0.7)
        //     {
        //         countRight++;
        //     }
        // }

        // Debug.Log("countLeft " + countLeft);
        // Debug.Log("countRight " + countRight);
        // if (countLeft > countRight)
        // {
        //     Point centre = new((int)outputYCircles[0, 0, indexMaxLeft, 0], (int)outputYCircles[0, 0, indexMaxLeft, 1]);  
        //     widthRectCircle = (int)outputYCircles[0, 0, indexMaxLeft, 2];
        //     centresCircles.Add(centre);
        // }
        // else
        // {
        //     Point centre = new((int)outputYCircles[0, 0, indexMaxRight, 0], (int)outputYCircles[0, 0, indexMaxRight, 1]);  
        //     widthRectCircle = -(int)outputYCircles[0, 0, indexMaxRight, 2];
        //     centresCircles.Add(centre);
        // }

        
    }

    void DrawCircles(Mat sourceImg)
    {
        Mat resultImg = sourceImg.clone();

        DetectPlayers(resultImg);
        DetectCircles(resultImg); // заполнение листа триплетами точек

        // foreach (Point pt in playersOnFrame)
        // {
        //     Imgproc.circle(resultImg, pt, 10, new Scalar(255, 0, 0), 5);
        // }

        
        // if (circles.Count == 1)
        // {
        //     centreFound = true;
        //     pitchCentre = new Point(circles[0].x + widthRectCircle / 2, circles[0].y);
        //     pitchCentres.Add(pitchCentre);
        //     Imgproc.circle(resultImg, pitchCentre, 10, new Scalar(255, 255, 255), 5);

        // }
        // else if (circles.Count == 2)
        // {
        //     centreFound = true;
        //     //pitchCentre = new Point((circles[0].x + circles[1].x) / 2, circles[0].y);
        //     pitchCentres.Add(pitchCentre);
        //     Imgproc.circle(resultImg, pitchCentre, 10, new Scalar(255, 255, 255), 5);
        // }
        // else
        // {
        //     centreFound = false;
        //     pitchCentres.Add(pitchCentre);
        // }
    }

    //Update is called once per frame
    void Update()
    {
        // поиск игроков ИИ
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // making a tensor out of a grayscale texture
            var channelCount = 3; //grayscale, 3 = color, 4 = color+alpha
            // Create a tensor for input from the texture.
            Imgproc.resize(img, img, new Size(1280, 1280));
            textureImg = new Texture2D(img.width(), img.height(), TextureFormat.RGBA32, false);
            Utils.matToTexture2D(img, textureImg, true, 0, true, false, false);
            Debug.Log(textureImg.width + " " + textureImg.height);
            Debug.Log(img.width() + " " + img.height());
            Tensor inputX = new Tensor(textureImg, channelCount);
            Debug.Log("SHAPE" + inputX.shape);

            // Peek at the output tensor without copying it.
            Tensor outputY = workerDetection.Execute(inputX).PeekOutput();
            Debug.Log("SHAPEEE" + outputY.shape);
            float probPlayer;
            bool addFlag = true;
            List<Point> centres = new List<Point>();
            for (int i = 0; i < 33600; i++)
            {
                probPlayer = outputY[0, 0, i, 7]; // names: ['ball', 'coach', 'goalkeeper', 'player', 'referee']
                if (probPlayer > 0.4) 
                {
                    Point centre = new((int)outputY[0, 0, i, 0], (int)outputY[0, 0, i, 1]); 
                    for (int n = 0; n < centres.Count; n++) 
                    {
                        if ((Math.Abs(centres[n].x - centre.x) >= 15) || (Math.Abs(centres[n].y - centre.y) >= 15))
                        {
                            addFlag = true;
                        }
                        else
                        {
                            addFlag = false;
                            break;
                        }
                    }
                    if (addFlag == true)
                    {
                        centres.Add(centre);
                        int widthRect = (int)outputY[0, 0, i, 2];
                        int heightRect = (int)outputY[0, 0, i, 3];
                        Point pt1 = new(centre.x - widthRect / 2, centre.y - heightRect / 2);
                        Point pt2 = new(centre.x + widthRect / 2, centre.y + heightRect / 2);
                        Imgproc.rectangle(img, pt1, pt2, new Scalar(255, 0, 0), 5); 
                        Imgproc.putText(img, probPlayer.ToString(), centre, 0, 1, new Scalar(255, 0, 0), 2);                   
                        //Imgproc.circle(img, centre, 3, new Scalar(255, 0, 0), 5);
                    }   
                }
            }
            
            foreach (Point pt in centres)
            {
                Debug.Log(pt);
            }

            Debug.Log(centres.Count);

            textureResult = new Texture2D(img.width(), img.height(), TextureFormat.RGBA32, false);
            Utils.matToTexture2D(img, textureResult, true, 0, true, false, false);
            _rawImage.texture = textureResult;
            // Dispose of the input tensor manually (not garbage-collected).
            inputX.Dispose();
        }
        // сегментация поля ИИ
        if (Input.GetKeyDown(KeyCode.S))
        {
            Mat imgDog = Imgcodecs.imread("D:/UnityProjects/TEST/Assets/Sprites/dog.jpg");
            Imgproc.resize(imgDog, imgDog, new Size(640, 640));
            textureImg = new Texture2D(imgDog.width(), imgDog.height(), TextureFormat.RGBA32, false);
            Utils.matToTexture2D(imgDog, textureImg, true, 0, true, false, false);
            var channelCount = 3; //grayscale, 3 = color, 4 = color+alpha
            Tensor inputX0 = new Tensor(textureImg, channelCount);
            // Peek at the output tensor without copying it.
            workerSegmentation.Execute(inputX0).PeekOutput();
            Tensor outputY0 = workerSegmentation.CopyOutput("output0");
            Tensor outputY1 = workerSegmentation.CopyOutput("output1");
            Debug.Log("SHAPEEE 0 " + outputY0.shape);
            Debug.Log("SHAPEEE 1 " + outputY1.shape);
            float currentSum, baseMask, mw, probDog;
            byte[] finalMask = new byte[160 * 160];
            Mat finalMaskMat = new (160, 160, CvType.CV_8UC1);
            List<Point> centres = new List<Point>();
            for (int j = 0; j < 8400; j++)
            {
                probDog = outputY0[0, 0, j, 20];
                if (probDog > 0.9)
                {
                    for (int r = 0; r < 160; r++)
                    {
                        for (int c = 0; c < 160; c++)
                        {
                            currentSum = 0;
                            for (int k = 0; k < 32; k++)
                            {
                                mw = outputY0[0, 0, j, k + 84];
                                baseMask = outputY1[0, r, c, k];
                                float item = mw * baseMask;
                                currentSum += item;
                            }
                            if (Sigmoid(currentSum) > 0.7)
                            {
                                finalMask[r * 160 + c] = 255;
                            }
                        }
                    }
                    finalMaskMat.put(0, 0, finalMask);
                    Imgcodecs.imwrite(String.Format("finalMask{0}.jpg", j), finalMaskMat); // записываем кадр в файл
                    Debug.Log("alarm");
                    Point centre = new((int)outputY0[0, 0, j, 0], (int)outputY0[0, 0, j, 1]); 
                    Debug.Log(centre.x + " " + centre.y);
                    int widthRect = (int)outputY0[0, 0, j, 2];
                    int heightRect = (int)outputY0[0, 0, j, 3];
                    Point pt1 = new(centre.x - widthRect / 2, centre.y - heightRect / 2);
                    Point pt2 = new(centre.x + widthRect / 2, centre.y + heightRect / 2);
                    Imgproc.rectangle(imgDog, pt1, pt2, new Scalar(255, 0, 0), 5);
                    
                    Mat mask = new (640, 640, CvType.CV_8UC1, new Scalar(0));
                    OpenCVForUnity.CoreModule.Rect rectForEmpty = new((int)pt1.x, (int)pt1.y, widthRect, heightRect);
                    Imgproc.rectangle(mask, rectForEmpty, new Scalar(255), -1);

                    Imgcodecs.imwrite("finalMaskMat.jpg", finalMaskMat);
                    Imgproc.resize(finalMaskMat, finalMaskMat, new Size(640, 640));
                    Imgcodecs.imwrite("finalMaskMatNEW.jpg", finalMaskMat);

                    Mat empty = new (640, 640, CvType.CV_8UC1, new Scalar(0));
                    Core.bitwise_and(finalMaskMat, mask, empty);
                    Mat final = new (640, 640, CvType.CV_8UC3, new Scalar(0,0,0));
                    Imgproc.cvtColor(imgDog, imgDog, Imgproc.COLOR_BGR2GRAY); 
                    Core.bitwise_and(imgDog, empty, imgDog);
                    Imgcodecs.imwrite("empty.jpg", empty); // записываем кадр в файл
                    Imgcodecs.imwrite("imgDog.jpg", imgDog); // записываем кадр в файл

                    
                    break;
                }
            }

        textureResult = new Texture2D(imgDog.width(), imgDog.height(), TextureFormat.RGBA32, false);
        Utils.matToTexture2D(imgDog, textureResult, true, 0, true, false, false);
        _rawImage.texture = textureResult;
        inputX0.Dispose();
        }
        // сегментация поля морфология
        if (Input.GetKeyDown(KeyCode.G))
        {
            img = FindBorders(img);
            textureResult = new Texture2D(img.width(), img.height(), TextureFormat.RGBA32, false);
            Utils.matToTexture2D(img, textureResult, true, 0, true, false, false);
            _rawImage.texture = textureResult;
        }
        // поиск центрального и штрафного кругов ИИ
        if (Input.GetKeyDown(KeyCode.F))
        {
            // making a tensor out of a grayscale texture
            var channelCount = 3; //grayscale, 3 = color, 4 = color+alpha
            // Create a tensor for input from the texture.
            Imgproc.resize(img, img, new Size(1280, 1280));
            textureImg = new Texture2D(img.width(), img.height(), TextureFormat.RGBA32, false);
            Utils.matToTexture2D(img, textureImg, true, 0, true, false, false);
            Debug.Log(textureImg.width + " " + textureImg.height);
            Tensor inputX = new Tensor(textureImg, channelCount);
            Debug.Log("SHAPE" + inputX.shape);

            // Peek at the output tensor without copying it.
            Tensor outputY = workerDetectionCircles.Execute(inputX).PeekOutput();
            Debug.Log("SHAPEEE" + outputY.shape);
            float probCircle;
            bool addFlag = true;
            List<Point> centres = new List<Point>();
            for (int i = 0; i < 33600; i++)
            {
                probCircle = outputY[0, 0, i, 9]; // names: ['18Yard', '18Yard Circle', '5Yard', 'First Half Central Circle', 'First Half Field', 'Second Half Central Circle', 'Second Half Field']
                if (probCircle > 0.6)
                {
                    Point centre = new((int)outputY[0, 0, i, 0], (int)outputY[0, 0, i, 1]); 
                    // int widthRect = (int)outputY[0, 0, i, 2];
                    // int heightRect = (int)outputY[0, 0, i, 3];
                    // Point pt1 = new(centre.x - widthRect / 2, centre.y - heightRect / 2);
                    // Point pt2 = new(centre.x + widthRect / 2, centre.y + heightRect / 2);
                    // Imgproc.rectangle(img, pt1, pt2, new Scalar(255, 0, 0), 5); 
                    for (int n = 0; n < centres.Count; n++) 
                    {
                        if ((Math.Abs(centres[n].x - centre.x) >= 15) || (Math.Abs(centres[n].y - centre.y) >= 15))
                        {
                            addFlag = true;
                        }
                        else
                        {
                            addFlag = false;
                            break;
                        }
                    }
                    if (addFlag == true)
                    {
                        centres.Add(centre);
                        int widthRect = (int)outputY[0, 0, i, 2];
                        int heightRect = (int)outputY[0, 0, i, 3];
                        Point pt1 = new(centre.x - widthRect / 2, centre.y - heightRect / 2);
                        Point pt2 = new(centre.x + widthRect / 2, centre.y + heightRect / 2);
                        Imgproc.rectangle(img, pt1, pt2, new Scalar(255, 0, 0), 5); 
                        Imgproc.putText(img, probCircle.ToString(), centre, 0, 1, new Scalar(255, 0, 0), 2);                   
                        Imgproc.circle(img, centre, 3, new Scalar(255, 0, 0), 5);
                        Debug.Log("123123123");
                    }   
                }
            }
            
            // foreach (Point pt in centres)
            // {
            //     Debug.Log(pt);
            // }

            //Debug.Log(centres.Count);

            textureResult = new Texture2D(img.width(), img.height(), TextureFormat.RGBA32, false);
            Utils.matToTexture2D(img, textureResult, true, 0, true, false, false);
            _rawImage.texture = textureResult;
            // Dispose of the input tensor manually (not garbage-collected).
            inputX.Dispose();
        }
    }

    Mat FindBorders(Mat img)
    {
        widthImg = img.cols();
        heightImg = img.rows();
        Mat result = new Mat(), dst = new Mat(), hsvMat = new Mat(), labMat = new Mat(), mask = new Mat(), mask1 = new Mat(), mask2 = new Mat(), imgGray = new Mat();
        Imgproc.cvtColor(img, imgGray, Imgproc.COLOR_BGR2GRAY);
        Imgproc.cvtColor(img, labMat, Imgproc.COLOR_BGR2Lab); 
        Imgproc.cvtColor(img, hsvMat, Imgproc.COLOR_BGR2HSV); 
        List<Mat> labChannels = new List<Mat>();
        Core.split(labMat, labChannels);
        Imgcodecs.imwrite("labChannels.jpg", labChannels[1]);

        Mat th = new (img.width(), img.height(), CvType.CV_8UC1);
        Imgproc.GaussianBlur(labChannels[1], th, new Size(15, 15), 0);
        Core.bitwise_not(th, th);
        Imgproc.threshold(th, th, 160, 255, Imgproc.THRESH_TOZERO_INV);
        Imgproc.threshold(th, th, 0, 255, Imgproc.THRESH_BINARY + Imgproc.THRESH_OTSU);

        // морфология
        Imgproc.morphologyEx(th, th, Imgproc.MORPH_ERODE, Imgproc.getStructuringElement(Imgproc.MORPH_ELLIPSE, new Size(15, 15)));
        Imgproc.morphologyEx(th, th, Imgproc.MORPH_ERODE, Imgproc.getStructuringElement(Imgproc.MORPH_ELLIPSE, new Size(15, 15)));
        Imgproc.morphologyEx(th, th, Imgproc.MORPH_ERODE, Imgproc.getStructuringElement(Imgproc.MORPH_ELLIPSE, new Size(15, 15)));
        
        Core.bitwise_not(th, th);
        List<MatOfPoint> contours = new List<MatOfPoint>();
        Mat hierarchy = new Mat();
        Imgproc.findContours(th, contours, hierarchy, Imgproc.RETR_LIST, Imgproc.CHAIN_APPROX_SIMPLE);
        int counterDilate = 0;

        while(contours.Count > 1)
        {
            contours.Clear();
            Imgproc.morphologyEx(th, th, Imgproc.MORPH_ERODE, Imgproc.getStructuringElement(Imgproc.MORPH_ELLIPSE, new Size(15, 15)));
            Imgproc.findContours(th, contours, hierarchy, Imgproc.RETR_LIST, Imgproc.CHAIN_APPROX_SIMPLE);
            counterDilate ++;
            //Imgcodecs.imwrite(String.Format("Assets/Video/fframe/fframe{0}.jpg", counterDilate), th);
        }
        for (int n = 0; n < counterDilate; n++)
        {
            Imgproc.morphologyEx(th, th, Imgproc.MORPH_DILATE, Imgproc.getStructuringElement(Imgproc.MORPH_ELLIPSE, new Size(20, 20)));
            //Imgcodecs.imwrite(String.Format("Assets/Video/fframe/ffframe{0}.jpg", counterFrame), th);
        }
        Imgproc.findContours(th, contours, hierarchy, Imgproc.RETR_LIST, Imgproc.CHAIN_APPROX_SIMPLE);

        while (contours.Count > 1)
        {
            contours.Clear();
            Imgproc.morphologyEx(th, th, Imgproc.MORPH_DILATE, Imgproc.getStructuringElement(Imgproc.MORPH_ELLIPSE, new Size(65, 20)));
            Imgproc.findContours(th, contours, hierarchy, Imgproc.RETR_LIST, Imgproc.CHAIN_APPROX_SIMPLE);            
        }

        
        
        // маска для DetectPlayers
        maskForPlayers = th.clone();
        Core.bitwise_not(maskForPlayers, maskForPlayers);
        Imgcodecs.imwrite(String.Format("Assets/Video/fframe/ffframe{0}.jpg", counterFrame), maskForPlayers);

        Core.bitwise_not(th, th);
        Core.bitwise_and(imgGray, th, imgGray);

        Imgproc.morphologyEx(th, th, Imgproc.MORPH_GRADIENT, Imgproc.getStructuringElement(Imgproc.MORPH_ELLIPSE, new Size(15, 15)));
        //Imgcodecs.imwrite("Assets/Video/fframe/MORPH_GRADIENT.jpg", th);

        // массив ненулевых пикселей
        Mat afterMorph = new Mat(), idx = new Mat();
        Core.findNonZero(th, idx);
        int[] idxArray = new int[idx.cols() * idx.rows() * idx.channels()];
        idx.get(0, 0, idxArray);
        //Debug.Log(idxArray.Length);

        // поиск левой точки через координату x
        int leftPointX = idxArray.Where((x,i) => i%2==0).Min();
        int indexLeftPointX = Array.IndexOf(idxArray, leftPointX);
        left = new Point(leftPointX, idxArray[indexLeftPointX+1]);
        Imgproc.circle(imgGray, left, 3, new Scalar(255), 3);

        // поиск верхней точки через координату y
        int upPointY = idxArray.Where((x,i) => i%2==1).Min();
        int indexUpPointY = Array.IndexOf(idxArray, upPointY);
        up = new Point(idxArray[indexUpPointY-1], upPointY);
        Imgproc.circle(imgGray, up, 3, new Scalar(160), 3);

        // поиск правой точки через координату x
        int rightPointX = idxArray.Where((x,i) => i%2==0).Max();
        int indexRightPointX = Array.IndexOf(idxArray, rightPointX);
        right = new Point(rightPointX, idxArray[indexRightPointX+1]);
        Imgproc.circle(imgGray, right, 3, new Scalar(90), 5);
        //Debug.Log("left " + left.x + " " + left.y + " up " + up.x + " " + up.y + " right " + right.x + " " + right.y);

        //---------------------------------------------------------------------------------------------------------------------
        if (Math.Abs(up.y - right.y) <= 50 && Math.Abs(up.y - left.y) <= 50 && Math.Abs(right.y - left.y) <= 50) // если точки очень близко друг к другу по оси y
        //(Math.Abs(up.x - left.x) < Math.Abs(up.x - widthImg/2))) || Math.Abs(up.x - right.x) < 10 || Math.Abs(up.x - left.x) < 10)
        {
            needKfY = true;
            //angle = 90;
            partOfPitch = 1;
            if (right.y <= left.y)
            {
                angle = 80;
            }
            else                
                angle = 100;
            //Debug.Log("ALARM"); 
        } 
        else if ((Math.Abs(up.x - left.x) <= 30 || Math.Abs(up.x - right.x) <= 30) && Math.Abs(left.y - right.y) <= 300) // если точки очень близко друг к другу по оси x
        {
            //Debug.Log("gg " + counterFrame);
            partOfPitch = 1;
            if (right.y <= left.y)
            {
                angle = 50;
            }
            else                
                angle = 130;
        }
        else if ((Math.Abs(up.x - left.x) <= 30 || Math.Abs(up.x - right.x) <= 30) && Math.Abs(left.y - right.y) >= 300) // если точки очень близко друг к другу по оси x
        {
            //Debug.Log("GGGGGGGGGGGGGGGG " + counterFrame);
            if (right.y <= left.y)
            {
                //Debug.Log("GGGGGGGGG123123GGGGGGG " + counterFrame);
                int length = 150;

                double hypotenuse = Math.Sqrt((heightImg - left.y)*(heightImg - left.y) + widthImg*widthImg);
                double sinAngle = (heightImg - left.y) / hypotenuse;
                double angle = Math.Asin(sinAngle) * 180 / Math.PI - 1;

                up.x = (int)(up.x + length * Math.Cos((180+angle) * Math.PI / 180));
                up.y = (int)(up.y + length * Math.Sin((180+angle) * Math.PI / 180));
                
                int x2 = (int)(left.x + length * Math.Cos((360-25) * Math.PI / 180));
                int y2 = (int)(left.y + length * Math.Sin((360-25) * Math.PI / 180));
                
                Point forLine = new Point(x2, y2);
                up = FindIntersection(right, up, forLine, left);
                partOfPitch = 0;
            }
            else         
            {
                int length = 150;

                double hypotenuse = Math.Sqrt((heightImg - right.y)*(heightImg - right.y) + widthImg*widthImg);
                double sinAngle = (heightImg - right.y) / hypotenuse;
                double angle = Math.Asin(sinAngle) * 180 / Math.PI - 1;

                up.x = (int)(up.x + length * Math.Cos((360-angle) * Math.PI / 180));
                up.y = (int)(up.y + length * Math.Sin((360-angle) * Math.PI / 180));
                
                int x2 = (int)(right.x + length * Math.Cos(25 * Math.PI / 180));
                int y2 = (int)(right.y + length * Math.Sin(25 * Math.PI / 180));
        
                Point forLine = new Point(x2, y2);
                up = FindIntersection(left, up, forLine, right);
                partOfPitch = 2;
                //Debug.Log("left " + left.x + " " + left.y + " up " + up.x + " " + up.y + " right " + right.x + " " + right.y);
            }       
        }
        else if ((counterFrame == 0 || historyOfParts[counterFrame - 1] != 0) &&
                ((up.x > widthImg/2 && right.y > left.y) || (up.x < widthImg/2 && Math.Abs(right.y - left.y) > 100) || (up.x > widthImg/2 && Math.Abs(right.y - left.y) <= 100)))
        {
            //Debug.Log("RIGHTright");
            int length = 150;
            angle = 30;
            int x2 = (int)(right.x + length * Math.Cos(angle * Math.PI / 180));
            int y2 = (int)(right.y + length * Math.Sin(angle * Math.PI / 180));
            //Debug.Log("left " + left.x + " " + left.y + " up " + up.x + " " + up.y + " right " + right.x + " " + right.y);
            Point forLine = new Point(x2, y2);
            up = FindIntersection(left, up, forLine, right);
            //Debug.Log("left " + left.x + " " + left.y + " up " + up.x + " " + up.y + " right " + right.x + " " + right.y);
            partOfPitch = 2;
            //isCentreForPers = false;
        }
        else if ((counterFrame == 0 || historyOfParts[counterFrame - 1] != 2) &&
                ((up.x < widthImg/2 && left.y > right.y) || (up.x > widthImg/2 && Math.Abs(right.y - left.y) > 100) || (up.x < widthImg/2 && Math.Abs(right.y - left.y) <= 100)))
        {
            //Debug.Log("LEFTleft");
            int length = 150;
            angle = 150;
            int x2 = (int)(left.x + length * Math.Cos(angle * Math.PI / 180));
            int y2 = (int)(left.y + length * Math.Sin(angle * Math.PI / 180));
            //Debug.Log("left " + left.x + " " + left.y + " up " + up.x + " " + up.y + " right " + right.x + " " + right.y);
            Point forLine = new Point(x2, y2);
            up = FindIntersection(right, up, forLine, left);
            
            partOfPitch = 0;
            //isCentreForPers = false;
        }

        historyOfParts.Add(partOfPitch);

        // // рисуем линию под углом из точки right
        // int length = 150;
        // int angle = 215;
        // int x2 = (int)(right.x + length * Math.Cos(angle * Math.PI / 180));
        // int y2 = (int)(right.y + length * Math.Sin(angle * Math.PI / 180));
        // Point forLine = new Point(x2, y2);
        // //Imgproc.line(imgGray, right, forLine, new Scalar(255), 10);
        //---------------------------------------------------------------------------------------------------------------------

        //Point newUP = FindIntersection(left, up, forLine, right);
        //Imgproc.line(imgGray, right, newUP, new Scalar(255), 10);
        

   
        //Imgproc.circle(imgGray, new Point(right.x, img.rows()-(left.y-right.y)), 3, new Scalar(255, 255, 255), 5);

        // Scalar lowGreen = new Scalar(40, 80, 30);
        // Scalar highGreen = new Scalar(70, 255, 153);
        // Core.inRange(hsvMat, lowGreen, highGreen, dst);
        // Imgcodecs.imwrite("dst.jpg", dst);

        return imgGray;
    }

    Mat TransformPerspective(Mat img)
    {
        Mat srcRectMat = new Mat(4, 1, CvType.CV_32FC2);
        Mat dstRectMat = new Mat(4, 1, CvType.CV_32FC2); 
        
        if (partOfPitch == 1)
        {
            int length = 150;
            if (right.y <= left.y)
            {
                int x2 = (int)(right.x + length * Math.Cos(angle * Math.PI / 180));
                int y2 = (int)(right.y + length * Math.Sin(angle * Math.PI / 180));
                Point forLine = new Point(x2, y2);
                Point rightDown = FindIntersection(new Point(left.x, img.rows()), new Point(right.x, img.rows() - (left.y - right.y)), forLine, right);

                srcRectMat.put(0, 0, left.x, left.y, right.x, right.y, left.x, img.rows(), rightDown.x, rightDown.y);

                Imgproc.circle(img, left, 30, new Scalar(255,255,255), 5);
                Imgproc.circle(img, right, 30, new Scalar(255,255,255), 5);
                Imgproc.circle(img, new Point(left.x, img.rows()), 30, new Scalar(255,255,255), 5);
                Imgproc.circle(img, rightDown, 30, new Scalar(255,255,255), 5);
                Imgcodecs.imwrite(String.Format("Assets/Video/2/bigImage2/frame{0}.jpg", counterFrame), img);
            }
            else
            {
                int x2 = (int)(left.x + length * Math.Cos(angle * Math.PI / 180));
                int y2 = (int)(left.y + length * Math.Sin(angle * Math.PI / 180));
                Point forLine = new Point(x2, y2);
                Point leftDown = FindIntersection(new Point(left.x, img.rows() - (right.y - left.y)), new Point(right.x, img.rows()), forLine, left);

                srcRectMat.put(0, 0, left.x, left.y, right.x, right.y, leftDown.x, leftDown.y, right.x, img.rows());

                Imgproc.circle(img, left, 30, new Scalar(255,255,255), 5);
                Imgproc.circle(img, right, 30, new Scalar(255,255,255), 5);
                Imgproc.circle(img, new Point(left.x, img.rows()), 30, new Scalar(255,255,255), 5);
                Imgproc.circle(img, leftDown, 30, new Scalar(255,255,255), 5);
                Imgcodecs.imwrite(String.Format("Assets/Video/2/bigImage2/frame{0}.jpg", counterFrame), img);
            }
            dstRectMat.put(0, 0, 0.0, 0.0, img.cols(), 0.0, 0.0, img.rows(), img.cols(), img.rows());
        }
        else if (partOfPitch == 2)
        {
            //Debug.Log("ALARM");
            // заносим точки в Mat для преобразования перспективы
            double DF = right.x - up.x;
            double AC = img.rows() - up.y;
            double AF = right.y - up.y;
            double BC = (DF * AC) / AF;
            double BE = up.x + BC - img.cols();

            // находим альфу
            double AB = Math.Sqrt(AC*AC + BC*BC);
            double sinAlpha = BC / AB;
            double alpha = Math.Asin(sinAlpha);
            //Debug.Log("alpha = " + alpha);  
            
            // находим бэту
            double IC = left.y - up.y;
            double AG = Math.Sqrt(IC*IC + up.x*up.x);
            double sinBeta = up.x / AG;
            double beta = Math.Asin(sinBeta);
            //Debug.Log("beta = " + beta);  

            // находим тэту и координаты точки K
            double theta = Math.PI - alpha - beta;
            double NB = (IC * (img.cols() + BE)) / up.x;
            double KB = (NB * sinBeta) / Math.Sin(theta);
            double BQ = (KB * BC) / AB;
            double KQ = (AC * KB) / AB;
            Point K = new Point((img.cols() + BE) - BQ, img.rows() - KQ);
            Point P = new Point(0, up.y + (img.rows() - K.y));
            int addRight = (int)(K.x - widthImg);
            int addLeft = (int)(K.x - up.x);
            listAddLeft.Add(addLeft);
            counterAddLeft++;

            Debug.Log("K " + ((img.cols() + BE) - BQ) + " " + (img.rows() - KQ) + " up " + up.x + " " + up.y);

            Mat bigImage = new Mat();

            // Core.copyMakeBorder(img, bigImage, 0, 0, 0, addRight, Core.BORDER_ISOLATED);
            // Imgproc.line(bigImage, new Point(left.x, img.rows()), K, new Scalar(0, 0, 255), 3);
            // Imgproc.line(bigImage, up, K, new Scalar(0, 0, 255), 3);
            // Imgproc.line(bigImage, left, up, new Scalar(0, 0, 255), 3);
            // Imgcodecs.imwrite(String.Format("Assets/Video/2/bigImage/frame{0}.jpg", counterFrame), bigImage); // записываем кадр в файл
            
            // srcRectMat.put(0, 0, left.x, left.y, up.x, up.y, left.x, img.rows(), K.x, K.y);
            // dstRectMat.put(0, 0, 0.0, 0.0, img.cols(), 0.0, 0.0, img.rows(), img.cols(), img.rows());
            
            Core.copyMakeBorder(img, bigImage, 0, 0, addLeft, addRight, Core.BORDER_ISOLATED);
            Imgproc.line(bigImage, new Point(left.x + addLeft, img.rows()), new Point(K.x + addLeft, K.y), new Scalar(255, 255, 255), 3);
            Imgproc.line(bigImage, new Point(up.x + addLeft, up.y), new Point(K.x + addLeft, K.y), new Scalar(255, 255, 255), 3);
            Imgproc.line(bigImage, new Point(left.x + addLeft, img.rows()), P, new Scalar(255, 255, 255), 3);
            Imgproc.circle(bigImage, P, 30, new Scalar(255,255,255), 5);
            Imgproc.circle(bigImage, new Point(up.x+addLeft, up.y), 30, new Scalar(255,255,255), 5);
            Imgproc.circle(bigImage, new Point(left.x+addLeft, img.rows()), 30, new Scalar(255,255,255), 5);
            Imgproc.circle(bigImage, new Point(K.x+addLeft, K.y), 30, new Scalar(255,255,255), 5);
            Imgcodecs.imwrite(String.Format("Assets/Video/2/bigImage2/frame{0}.jpg", counterFrame), bigImage); // записываем кадр в файл

            srcRectMat.put(0, 0, P.x, P.y, up.x+addLeft, up.y, left.x+addLeft, img.rows(), K.x+addLeft, K.y);
            dstRectMat.put(0, 0, 0.0, 0.0, img.cols(), 0.0, 0.0, img.rows(), img.cols(), img.rows());
            img = bigImage;
        }
        else if (partOfPitch == 0)
        {
            // сохраняем значения координат точек
            Point specUp = new Point(up.x, up.y);
            Point specLeft = new Point(left.x, left.y);
            Point specRight = new Point(right.x, right.y);

            // расширяем изображение слева, так как увели влево левую точку
            int microAddLeft = Math.Abs((int)left.x);
            Mat bigImage = new Mat();
            Core.copyMakeBorder(img, bigImage, 0, 0, microAddLeft, 0, Core.BORDER_ISOLATED); 

            up = new Point(bigImage.cols() - (up.x + microAddLeft), up.y);
            left = new Point(0, specRight.y);
            right = new Point(right.x + microAddLeft, specLeft.y);            

            //Debug.Log("left " + left.x + " " + left.y + " up " + up.x + " " + up.y + " right " + right.x + " " + right.y);
            //Debug.Log("ALARM");
            // заносим точки в Mat для преобразования перспективы
            double DF = right.x - up.x;
            double AC = bigImage.rows() - up.y;
            double AF = right.y - up.y;
            double BC = (DF * AC) / AF;
            double BE = up.x + BC - bigImage.cols();

            // находим альфу
            double AB = Math.Sqrt(AC*AC + BC*BC);
            double sinAlpha = BC / AB;
            double alpha = Math.Asin(sinAlpha);
            //Debug.Log("alpha = " + alpha);  
            
            // находим бэту
            double IC = left.y - up.y;
            double AG = Math.Sqrt(IC*IC + up.x*up.x);
            double sinBeta = up.x / AG;
            double beta = Math.Asin(sinBeta);
            //Debug.Log("beta = " + beta);  

            // находим тэту и координаты точки K
            double theta = Math.PI - alpha - beta;
            double NB = (IC * (bigImage.cols() + BE)) / up.x;
            double KB = (NB * sinBeta) / Math.Sin(theta);
            double BQ = (KB * BC) / AB;
            double KQ = (AC * KB) / AB;
            Point K = new Point((bigImage.cols() + BE) - BQ, bigImage.rows() - KQ);
            Point P = new Point(0, up.y + (bigImage.rows() - K.y));
            int addRight = (int)(K.x - bigImage.cols());
            int addLeft = (int)(K.x - up.x);
            
            listAddLeft.Add(addLeft);
            counterAddLeft++;
            Debug.Log("listAddLeft.Count123 " + listAddLeft.Count);
            //Debug.Log("addRight = " + addRight);  
            //Debug.Log("addLeft = " + addLeft);  

            up = new Point(specUp.x + microAddLeft + addLeft, specUp.y);
            left = new Point(specLeft.x, specLeft.y);
            right = new Point(specRight.x + microAddLeft + addLeft, specRight.y);
            P = new Point(bigImage.cols() + addRight + addLeft , P.y);
            K = new Point(0, K.y);
            

            //Debug.Log("K " + ((bigImage.cols() + BE) - BQ) + " " + (bigImage.rows() - KQ) + " up " + up.x + " " + up.y);

            

            // Core.copyMakeBorder(img, bigImage, 0, 0, 0, addRight, Core.BORDER_ISOLATED);
            // Imgproc.line(bigImage, new Point(left.x, img.rows()), K, new Scalar(0, 0, 255), 3);
            // Imgproc.line(bigImage, up, K, new Scalar(0, 0, 255), 3);
            // Imgproc.line(bigImage, left, up, new Scalar(0, 0, 255), 3);
            // Imgcodecs.imwrite(String.Format("Assets/Video/2/bigImage/frame{0}.jpg", counterFrame), bigImage); // записываем кадр в файл
            
            // srcRectMat.put(0, 0, left.x, left.y, up.x, up.y, left.x, img.rows(), K.x, K.y);
            // dstRectMat.put(0, 0, 0.0, 0.0, img.cols(), 0.0, 0.0, img.rows(), img.cols(), img.rows());
            
            Core.copyMakeBorder(bigImage, bigImage, 0, 0, addLeft, addRight, Core.BORDER_ISOLATED);
            // Imgproc.line(bigImage, new Point(left.x + addLeft, img.rows()), new Point(K.x + addLeft, K.y), new Scalar(255, 255, 255), 3);
            // Imgproc.line(bigImage, new Point(up.x + addLeft, up.y), new Point(K.x + addLeft, K.y), new Scalar(255, 255, 255), 3);
            // Imgproc.line(bigImage, new Point(left.x + addLeft, img.rows()), P, new Scalar(255, 255, 255), 3);
            Imgproc.circle(bigImage, P, 30, new Scalar(255,255,255), 5);
            Imgproc.circle(bigImage, up, 30, new Scalar(255,255,255), 5);
            Imgproc.circle(bigImage, new Point(right.x, img.rows()), 30, new Scalar(255,255,255), 5);
            Imgproc.circle(bigImage, K, 30, new Scalar(255,255,255), 5);
            Imgcodecs.imwrite(String.Format("Assets/Video/2/bigImage2/frame{0}.jpg", counterFrame), bigImage); // записываем кадр в файл

            
            srcRectMat.put(0, 0, up.x, up.y, P.x, P.y, K.x, K.y, right.x, img.rows());
            dstRectMat.put(0, 0, 0.0, 0.0, img.cols(), 0.0, 0.0, img.rows(), img.cols(), img.rows());
            img = bigImage;
        }
        
        Mat imgNormalPerspective = Perspective(img, srcRectMat, dstRectMat);

        //Imgproc.circle(imgNormalPerspective, pitchCentreOR18yard[counterFrame], 20, new Scalar(255), 5);
        return imgNormalPerspective;
    }

    Mat Perspective(Mat img, Mat pts1, Mat pts2) // Преобразование перспективы
    {
        Mat result = new Mat();
        matrixPerspectiveTransform  = Imgproc.getPerspectiveTransform(pts1, pts2);
        Imgproc.warpPerspective(img, result, matrixPerspectiveTransform, new Size(widthImg, heightImg));

        // // Перерасчет центра поля, либо центра 18yard
        // Mat globalCentreMat = new Mat(3, 1, CvType.CV_64FC1);
        // globalCentreMat.put(0, 0, pitchCentreOR18yard[counterFrame].x, pitchCentreOR18yard[counterFrame].y, 1);
        // pitchCentreOR18yard[counterFrame] = CalculateNewCoords(pitchCentreOR18yard[counterFrame], perspectiveTransform);
        
        return result;
    }

    void CalculateNewCoords(List<List<Point>> listPlayersCoords, List<List<Point>> listCirclesCoords, List<List<Point>> listCentresCoordsResult,  Mat matrixTransform, Mat imgTest)
    {
        listCentresCoordsResult.Add(new List<Point>());

        double M00 = matrixTransform.get(0,0)[0];
        double M01 = matrixTransform.get(0,1)[0];
        double M02 = matrixTransform.get(0,2)[0];
        double M10 = matrixTransform.get(1,0)[0];
        double M11 = matrixTransform.get(1,1)[0];
        double M12 = matrixTransform.get(1,2)[0];
        double M20 = matrixTransform.get(2,0)[0];
        double M21 = matrixTransform.get(2,1)[0];
        double M22 = matrixTransform.get(2,2)[0];
        
        for (int p = 0; p < listPlayersCoords[counterFrame].Count; p++)
        {
            double x = listPlayersCoords[counterFrame][p].x;
            double y = listPlayersCoords[counterFrame][p].y;
            listPlayersCoords[counterFrame][p].x = (int)((M00*x + M01*y + M02) / (M20*x + M21*y + M22));
            listPlayersCoords[counterFrame][p].y = (int)((M10*x + M11*y + M12) / (M20*x + M21*y + M22));
            // pt.x = (int)((M00*x + M01*y + M02) / (M20*x + M21*y + M22));
            // pt.y = (int)((M10*x + M11*y + M12) / (M20*x + M21*y + M22));
        }
        for (int c = 0; c < 3; c++)
        {
            double x = listCirclesCoords[counterFrame][c].x;
            double y = listCirclesCoords[counterFrame][c].y;
            int newX = (int)((M00*x + M01*y + M02) / (M20*x + M21*y + M22));
            int newY = (int)((M10*x + M11*y + M12) / (M20*x + M21*y + M22));
            listCentresCoordsResult[counterFrame].Add(new Point(newX, newY));
            // pt.x = (int)((M00*x + M01*y + M02) / (M20*x + M21*y + M22));
            // pt.y = (int)((M10*x + M11*y + M12) / (M20*x + M21*y + M22));
        }

        foreach (Point pt in listPlayersCoords[counterFrame]) 
        {
            Imgproc.circle(imgTest, pt, 3, new Scalar(255, 255, 255), 10);
        }
        foreach (Point pt in listCentresCoordsResult[counterFrame]) 
        {
            Imgproc.circle(imgTest, pt, 3, new Scalar(255, 255, 255), 10);
        }

        Imgcodecs.imwrite(String.Format("Assets/Video/"+ numberVideo.ToString() +"/imgTest/frame{0}.jpg", counterFrame), imgTest); // записываем кадр в файл
    }

    void CalculateCoords2DScheme(List<List<Point>> listPlayersCoords, List<List<Point>> listCirclesCoords)
    {
        Point centreUp = new Point(640, 300);
        Point centreDown = new Point(640, 530);
        Point centreRight = new Point(755, 415);
        Point centreLeft = new Point(525, 415);

        Point up18yardLeft = new Point(200, 330);
        Point centre18yardLeft = new Point(250, 415);
        Point down18yardLeft = new Point(200, 500);

        Point up18yardRight = new Point(1080, 330);
        Point centre18yardRight = new Point(1030, 415);
        Point down18yardRight = new Point(1080, 500);

        for (int p = 0; p < listPlayersCoords[counterFrame].Count; p++)
        {
            double x = listPlayersCoords[counterFrame][p].x;
            double y = listPlayersCoords[counterFrame][p].y;
            double kfX, kfY, kfYforHeight;
            if (whatSee == 0 || whatSee == 1)
            {
                if (whatSee == 0) // центр - левая точка круга
                {
                    kfX = (x - listCirclesCoords[counterFrame][2].x) / ((listCirclesCoords[counterFrame][0].x + listCirclesCoords[counterFrame][1].x)/2 - listCirclesCoords[counterFrame][2].x);
                    listPlayersCoords[counterFrame][p].x = (int)(centreLeft.x + (centreUp.x - centreLeft.x)*kfX);
                }         
                else if (whatSee == 1) // центр - правая точка круга
                {
                    kfX = (x - listCirclesCoords[counterFrame][0].x) / (listCirclesCoords[counterFrame][2].x - (listCirclesCoords[counterFrame][0].x + listCirclesCoords[counterFrame][1].x)/2);
                    listPlayersCoords[counterFrame][p].x = (int)(centreUp.x + (centreRight.x - centreUp.x)*kfX);
                }

                kfY = (y - listCirclesCoords[counterFrame][0].y) / (listCirclesCoords[counterFrame][1].y - listCirclesCoords[counterFrame][0].y);
                if (needKfY)
                {
                    kfYforHeight = 1+Math.Pow(100, y/heightImg-0.9)*0.4;
                }
                else
                    kfYforHeight = 1+Math.Pow(100, y/heightImg-0.9)*0.15;
                listPlayersCoords[counterFrame][p].y = (int)((centreUp.y + (centreDown.y - centreUp.y)*kfY) / kfYforHeight);
            }

            else if (whatSee == 2 || whatSee == 3) 
            {
                kfY = (y - listCirclesCoords[counterFrame][0].y) / (listCirclesCoords[counterFrame][1].y - listCirclesCoords[counterFrame][0].y);
                if (whatSee == 2) // левая штрафная
                {
                    kfX = (x - listCirclesCoords[counterFrame][0].x) / (listCirclesCoords[counterFrame][2].x - (listCirclesCoords[counterFrame][0].x + listCirclesCoords[counterFrame][1].x)/2);
                    listPlayersCoords[counterFrame][p].x = (int)(up18yardLeft.x + (centre18yardLeft.x - up18yardLeft.x)*kfX);
                    listPlayersCoords[counterFrame][p].y = (int)(up18yardLeft.y + (down18yardLeft.y - up18yardLeft.y)*kfY);
                }
                else if (whatSee == 3) // правая штрафная
                {
                    kfX = (x - listCirclesCoords[counterFrame][2].x) / ((listCirclesCoords[counterFrame][0].x + listCirclesCoords[counterFrame][1].x)/2 - listCirclesCoords[counterFrame][2].x);
                    listPlayersCoords[counterFrame][p].x = (int)(centre18yardRight.x + (up18yardRight.x - centre18yardRight.x)*kfX);
                    listPlayersCoords[counterFrame][p].y = (int)(up18yardRight.y + (down18yardRight.y - up18yardRight.y)*kfY);
                }
            }
        }
        needKfY = false;
    }

    Point FindIntersection(Point pt1, Point pt2, Point pt3, Point pt4)
    {
        Point result = new Point();
        double a1, a2, b1, b2, c1, c2;
        a1 = pt1.y - pt2.y;
        b1 = pt2.x - pt1.x;
        c1 = pt1.x * pt2.y - pt2.x * pt1.y;
        a2 = pt3.y - pt4.y;
        b2 = pt4.x - pt3.x;
        c2 = pt3.x * pt4.y - pt4.x * pt3.y;

        if (Parall(a1, a2, b1, b2))
            return result;
        else
        {
            Intersection(a1, a2, b1, b2, c1, c2, out result.x, out result.y);
            return result;
        }

    }

    void Intersection(double a1, double a2, double b1, double b2, double c1, double c2, out double x, out double y)
    {
        double det = a1 * b2 - a2 * b1;
        x = (b1 * c2 - b2 * c1) / det;
        y = (a2 * c1 - a1 * c2) / det;
    }

    bool Parall(double a1, double a2, double b1, double b2)
    {
        if ((a1 / a2) == (b1 / b2))
        {
            return true;
        }
        else
            return false;
    }

    public static float Sigmoid(float value) 
    {
        return 1.0f / (1.0f + (float) Math.Exp(-value));
    }

    private void OnDestroy()
    {
        // Dispose of the engine manually (not garbage-collected).
        workerDetection?.Dispose();
        workerSegmentation?.Dispose();
        workerDetectionCircles?.Dispose();
    }


}
