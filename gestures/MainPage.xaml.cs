using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace gestures
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        
        ManipulationInputProcessor manipulationProcessor;
        ManipulationInputProcessorForScroll manipulationProcessorForScroll;

        GestureRecognizer scrollRecognizer;

        public MainPage()
        {
            this.InitializeComponent();


            manipulationProcessorForScroll = new ManipulationInputProcessorForScroll(scrollRecognizer = new GestureRecognizer(), manipulateMeScroll, mainCanvas, statusScroll, ObjectName);
            manipulationProcessorForScroll.UseInertia(true);


            // Create a ManipulationInputProcessor which will listen for events on the
            // rectangle, process them, and update the rectangle's position, size, and rotation
            manipulationProcessor = new ManipulationInputProcessor(new GestureRecognizer(), scrollRecognizer, gridImage1, mainCanvas, status, ObjectName);
            manipulationProcessor.GridHandler = decorationImage1;
            manipulationProcessor = new ManipulationInputProcessor(new GestureRecognizer(), scrollRecognizer, resize1, mainCanvas, status, ObjectName, gridImage1 );
            manipulationProcessor.UseInertia(true);
            manipulationProcessor = new ManipulationInputProcessor(new GestureRecognizer(), scrollRecognizer, manipulateMeImage2, mainCanvas, status, ObjectName);
            manipulationProcessor.UseInertia(true);
            manipulationProcessor = new ManipulationInputProcessor(new GestureRecognizer(), scrollRecognizer, manipulateMeImage3, mainCanvas, status, ObjectName);
            manipulationProcessor.UseInertia(true);
            manipulationProcessor = new ManipulationInputProcessor(new GestureRecognizer(), scrollRecognizer, manipulateMeImage4, mainCanvas, status, ObjectName);
            manipulationProcessor.UseInertia(true);
            manipulationProcessor = new ManipulationInputProcessor(new GestureRecognizer(), scrollRecognizer, manipulateMeImage5, mainCanvas, status, ObjectName);
            manipulationProcessor.UseInertia(true);
            manipulationProcessor = new ManipulationInputProcessor(new GestureRecognizer(), scrollRecognizer, manipulateMeImage6, mainCanvas, status, ObjectName);
            manipulationProcessor.UseInertia(true);
            manipulationProcessor = new ManipulationInputProcessor(new GestureRecognizer(), scrollRecognizer, manipulateMeImage7, mainCanvas, status, ObjectName);
            manipulationProcessor.UseInertia(true);

        }
    }

    class ManipulationInputProcessor
    {
        public GestureRecognizer recognizer;
        public GestureRecognizer scrollRecognizer;
        UIElement element;
        public FrameworkElement elementResize = null;
        UIElement reference;
        public Grid GridHandler;
        TransformGroup cumulativeTransform;
        MatrixTransform previousTransform;
        CompositeTransform deltaTransform;
        int numPointers = 0;

        TextBlock statusTextBlock;
        TextBlock nameTextBlock;

        string status
        {
            get => statusTextBlock.Text;
            set
            {
                if (numPointers == 0)
                    statusTextBlock.Text = $"Image Event: {value}";
                else
                    statusTextBlock.Text = $"Image Event: {value} Pointers: {numPointers}";

                Debug.WriteLine(statusTextBlock.Text);
            }
        }

        public ManipulationInputProcessor(GestureRecognizer gestureRecognizer, GestureRecognizer scrollRecognizer, UIElement target, UIElement referenceFrame, TextBlock statusText, TextBlock objectNameText, FrameworkElement _elementResize = null)
        {
            recognizer = gestureRecognizer;
            this.scrollRecognizer = scrollRecognizer;
            element = target;
            elementResize = _elementResize;
            reference = referenceFrame;
            statusTextBlock = statusText;
            nameTextBlock = objectNameText;

            // Initialize the transforms that will be used to manipulate the shape
            InitializeTransforms();

            // The GestureSettings property dictates what manipulation events the
            // Gesture Recognizer will listen to.  This will set it to a limited
            // subset of these events.
            recognizer.GestureSettings = GenerateDefaultSettings();
            recognizer.CrossSlideHorizontally = true;
            LockToYAxis();


            // Set up pointer event handlers. These receive input events that are used by the gesture recognizer.
            element.PointerPressed += OnPointerPressed;
            element.PointerMoved += OnPointerMoved;
            element.PointerReleased += OnPointerReleased;
            element.PointerCanceled += OnPointerCanceled;

            // Set up event handlers to respond to gesture recognizer output
            recognizer.ManipulationStarted += OnManipulationStarted;
            recognizer.ManipulationUpdated += OnManipulationUpdated;
            recognizer.ManipulationCompleted += OnManipulationCompleted;
            recognizer.ManipulationInertiaStarting += OnManipulationInertiaStarting;
            recognizer.CrossSliding += OnCrossSliding;
            recognizer.Dragging += OnDragging;
            recognizer.Holding += OnHolding;
            recognizer.RightTapped += OnRightTapped;
            recognizer.Tapped += OnTapped;
        }

        public void InitializeTransforms()
        {
            cumulativeTransform = new TransformGroup();
            deltaTransform = new CompositeTransform();
            previousTransform = new MatrixTransform() { Matrix = Matrix.Identity };

            cumulativeTransform.Children.Add(previousTransform);
            cumulativeTransform.Children.Add(deltaTransform);

            if (elementResize == null)
                element.RenderTransform = cumulativeTransform;
            else
                elementResize.RenderTransform = cumulativeTransform;
        }

        // Return the default GestureSettings for this sample
        GestureSettings GenerateDefaultSettings()
        {
            return
                GestureSettings.ManipulationTranslateX |
                GestureSettings.ManipulationTranslateY |
                //GestureSettings.ManipulationRotate |
                GestureSettings.ManipulationTranslateInertia |
                //GestureSettings.ManipulationRotateInertia |
                GestureSettings.ManipulationScale |
                GestureSettings.CrossSlide |
                GestureSettings.Tap |
                GestureSettings.DoubleTap |
                GestureSettings.RightTap |
                GestureSettings.Hold |
                GestureSettings.Drag;
        }

        // Route the pointer pressed event to the gesture recognizer.
        // The points are in the reference frame of the canvas that contains the rectangle element.
        void OnPointerPressed(object sender, PointerRoutedEventArgs args)
        {
            // Set the pointer capture to the element being interacted with so that only it
            // will fire pointer-related events
            //scrollRecognizer.CompleteGesture();

            element.CapturePointer(args.Pointer);

            // Feed the current point into the gesture recognizer as a down event
            recognizer.ProcessDownEvent(args.GetCurrentPoint(reference));

            nameTextBlock.Text = ((FrameworkElement)sender).Name;
            ++numPointers;
            args.Handled = true;
        }

        // Route the pointer moved event to the gesture recognizer.
        // The points are in the reference frame of the canvas that contains the rectangle element.
        void OnPointerMoved(object sender, PointerRoutedEventArgs args)
        {
            // Feed the set of points into the gesture recognizer as a move event
            recognizer.ProcessMoveEvents(args.GetIntermediatePoints(reference));
            nameTextBlock.Text = ((FrameworkElement)sender).Name;
            args.Handled = true;

        }

        // Route the pointer released event to the gesture recognizer.
        // The points are in the reference frame of the canvas that contains the rectangle element.
        void OnPointerReleased(object sender, PointerRoutedEventArgs args)
        {
            // Feed the current point into the gesture recognizer as an up event
            recognizer.ProcessUpEvent(args.GetCurrentPoint(reference));

            // Release the pointer
            element.ReleasePointerCapture(args.Pointer);
            nameTextBlock.Text = ((FrameworkElement)sender).Name;

            --numPointers;
            args.Handled = true;
        }

        // Route the pointer canceled event to the gesture recognizer.
        // The points are in the reference frame of the canvas that contains the rectangle element.
        void OnPointerCanceled(object sender, PointerRoutedEventArgs args)
        {
            recognizer.CompleteGesture();
            element.ReleasePointerCapture(args.Pointer);
            nameTextBlock.Text = ((FrameworkElement)sender).Name;
            --numPointers;
            args.Handled = true;
        }

        // When a manipulation begins, change the color of the object to reflect
        // that a manipulation is in progress
        void OnManipulationStarted(object sender, ManipulationStartedEventArgs e)
        {
            //Border b = element as Border;
            //b.Background = new SolidColorBrush(Windows.UI.Colors.DeepSkyBlue);
        }

        // Process the change resulting from a manipulation
        void OnManipulationUpdated(object sender, ManipulationUpdatedEventArgs e)
        {
            if (elementResize == null)
            {
                previousTransform.Matrix = cumulativeTransform.Value;

                // Get the center point of the manipulation for rotation
                Point center = new Point(e.Position.X, e.Position.Y);
                deltaTransform.CenterX = center.X;
                deltaTransform.CenterY = center.Y;

                // Look at the Delta property of the ManipulationDeltaRoutedEventArgs to retrieve
                // the rotation, X, and Y changes
                deltaTransform.Rotation = e.Delta.Rotation;
                deltaTransform.TranslateX = e.Delta.Translation.X;
                deltaTransform.TranslateY = e.Delta.Translation.Y;
            } else
            {
                previousTransform.Matrix = cumulativeTransform.Value;

                // Get the center point of the manipulation for rotation
                //Point center = new Point(e.Position.X, e.Position.Y);
                //deltaTransform.CenterX = center.X;
                //deltaTransform.CenterY = center.Y;

                // Look at the Delta property of the ManipulationDeltaRoutedEventArgs to retrieve
                // the rotation, X, and Y changes
                
                //deltaTransform.ScaleX = e.Delta.Translation.X/elementResize.Width;
                deltaTransform.ScaleY = 1 + (e.Delta.Translation.Y / elementResize.ActualHeight);
            }

            string text = "";

            if (e.Delta.Scale != 1)
                text = text + "Pinching ";
            else if (e.Delta.Translation.X != 0 || e.Delta.Translation.Y != 0)
                text = text + "Scrolling ";

            status = text;
        }

        // When a manipulation that's a result of inertia begins, change the color of the
        // the object to reflect that inertia has taken over
        void OnManipulationInertiaStarting(object sender, ManipulationInertiaStartingEventArgs e)
        {
            //Border b = element as Border;
            //b.Background = new SolidColorBrush(Windows.UI.Colors.RoyalBlue);
        }

        // When a manipulation has finished, reset the color of the object
        void OnManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            //Border b = element as Border;
            //b.Background = new SolidColorBrush(Windows.UI.Colors.LightGray);

            numPointers = 0;
            //statusText.Text = "Move/Rotate";
        }

        private void OnCrossSliding(GestureRecognizer sender, CrossSlidingEventArgs args)
        { 
            status = "Swipe";
        }


        private void OnTapped(GestureRecognizer sender, TappedEventArgs args)
        {
            if (args.TapCount <= 1)
            {
                status = "Tapped";
                if (GridHandler != null)
                {
                    if (GridHandler.Visibility == Visibility.Visible)
                        GridHandler.Visibility = Visibility.Collapsed;
                    else
                        GridHandler.Visibility = Visibility.Visible;
                }
            }
            else
                status = "Double Tapped";
        }

        private void OnRightTapped(GestureRecognizer sender, RightTappedEventArgs args)
        {
            status = "Right Tapped";
        }

        private void OnHolding(GestureRecognizer sender, HoldingEventArgs args)
        {
            status = "Holding";
        }

        private void OnDragging(GestureRecognizer sender, DraggingEventArgs args)
        {
            status = "Dragging";
        }

        // Modify the GestureSettings property to only allow movement on the X axis
        public void LockToXAxis()
        {
            recognizer.CompleteGesture();
            recognizer.GestureSettings |= GestureSettings.ManipulationTranslateY | GestureSettings.ManipulationTranslateX;
            recognizer.GestureSettings ^= GestureSettings.ManipulationTranslateY;
        }

        // Modify the GestureSettings property to only allow movement on the Y axis
        public void LockToYAxis()
        {
            recognizer.CompleteGesture();
            recognizer.GestureSettings |= GestureSettings.ManipulationTranslateY | GestureSettings.ManipulationTranslateX;
            recognizer.GestureSettings ^= GestureSettings.ManipulationTranslateX;
        }

        // Modify the GestureSettings property to allow movement on both the the X and Y axes
        public void MoveOnXAndYAxes()
        {
            recognizer.CompleteGesture();
            recognizer.GestureSettings |= GestureSettings.ManipulationTranslateX | GestureSettings.ManipulationTranslateY;
        }

        // Modify the GestureSettings property to enable or disable inertia based on the passed-in value
        public void UseInertia(bool inertia)
        {
            if (!inertia)
            {
                recognizer.CompleteGesture();
                recognizer.GestureSettings ^= GestureSettings.ManipulationTranslateInertia | GestureSettings.ManipulationRotateInertia;
            }
            else
            {
                recognizer.GestureSettings |= GestureSettings.ManipulationTranslateInertia | GestureSettings.ManipulationRotateInertia;
            }
        }

        public void Reset()
        {
            element.RenderTransform = null;
            recognizer.CompleteGesture();
            InitializeTransforms();
            recognizer.GestureSettings = GenerateDefaultSettings();
        }
    }

    class ManipulationInputProcessorForScroll
    {
        public GestureRecognizer recognizer;
        ScrollViewer element;
        UIElement reference;
        TransformGroup cumulativeTransform;
        MatrixTransform previousTransform;
        CompositeTransform deltaTransform;
        int numPointers = 0;

        TextBlock statusTextBlock;
        TextBlock nameTextBlock;

        string status
        {
            get => statusTextBlock.Text;
            set
            {
                if (numPointers == 0)
                    statusTextBlock.Text = $"Scroll event: {value}";
                else
                    statusTextBlock.Text = $"Scroll event: {value} Pointers: {numPointers}";

                Debug.WriteLine(statusTextBlock.Text);
            }
        }

        public ManipulationInputProcessorForScroll(GestureRecognizer gestureRecognizer, ScrollViewer target, UIElement referenceFrame, TextBlock statusText, TextBlock objectNameText)
        {
            recognizer = gestureRecognizer;
            element = target;
            reference = referenceFrame;
            statusTextBlock = statusText;
            nameTextBlock = objectNameText;

            // Initialize the transforms that will be used to manipulate the shape
            InitializeTransforms();

            // The GestureSettings property dictates what manipulation events the
            // Gesture Recognizer will listen to.  This will set it to a limited
            // subset of these events.
            recognizer.GestureSettings = GenerateDefaultSettings();
            recognizer.CrossSlideHorizontally = true;
            LockToYAxis();


            // Set up pointer event handlers. These receive input events that are used by the gesture recognizer.
            element.PointerPressed += OnPointerPressed;
            element.PointerMoved += OnPointerMoved;
            element.PointerReleased += OnPointerReleased;
            element.PointerCanceled += OnPointerCanceled;

            // Set up event handlers to respond to gesture recognizer output
            recognizer.ManipulationStarted += OnManipulationStarted;
            recognizer.ManipulationUpdated += OnManipulationUpdated;
            recognizer.ManipulationCompleted += OnManipulationCompleted;
            recognizer.ManipulationInertiaStarting += OnManipulationInertiaStarting;
            recognizer.CrossSliding += OnCrossSliding;
            recognizer.Dragging += OnDragging;
            recognizer.Holding += OnHolding;
            recognizer.RightTapped += OnRightTapped;
            recognizer.Tapped += OnTapped;
        }

        public void InitializeTransforms()
        {
            cumulativeTransform = new TransformGroup();
            deltaTransform = new CompositeTransform();
            previousTransform = new MatrixTransform() { Matrix = Matrix.Identity };

            cumulativeTransform.Children.Add(previousTransform);
            cumulativeTransform.Children.Add(deltaTransform);

            //element.RenderTransform = cumulativeTransform;
        }

        // Return the default GestureSettings for this sample
        GestureSettings GenerateDefaultSettings()
        {
            return
                GestureSettings.ManipulationTranslateX |
                //GestureSettings.ManipulationTranslateY |
                //GestureSettings.ManipulationRotate |
                //GestureSettings.ManipulationTranslateInertia |
                //GestureSettings.ManipulationRotateInertia |
                GestureSettings.ManipulationScale |
                GestureSettings.CrossSlide |
                GestureSettings.Tap |
                GestureSettings.DoubleTap |
                GestureSettings.RightTap |
                GestureSettings.Hold |
                GestureSettings.Drag;
        }

        // Route the pointer pressed event to the gesture recognizer.
        // The points are in the reference frame of the canvas that contains the rectangle element.
        void OnPointerPressed(object sender, PointerRoutedEventArgs args)
        {
            // Set the pointer capture to the element being interacted with so that only it
            // will fire pointer-related events
            element.CapturePointer(args.Pointer);

            // Feed the current point into the gesture recognizer as a down event
            recognizer.ProcessDownEvent(args.GetCurrentPoint(reference));

            nameTextBlock.Text = ((FrameworkElement)sender).Name;
            ++numPointers;
        }

        // Route the pointer moved event to the gesture recognizer.
        // The points are in the reference frame of the canvas that contains the rectangle element.
        void OnPointerMoved(object sender, PointerRoutedEventArgs args)
        {
            // Feed the set of points into the gesture recognizer as a move event
            recognizer.ProcessMoveEvents(args.GetIntermediatePoints(reference));
            nameTextBlock.Text = ((FrameworkElement)sender).Name;

        }

        // Route the pointer released event to the gesture recognizer.
        // The points are in the reference frame of the canvas that contains the rectangle element.
        void OnPointerReleased(object sender, PointerRoutedEventArgs args)
        {
            // Feed the current point into the gesture recognizer as an up event
            recognizer.ProcessUpEvent(args.GetCurrentPoint(reference));

            // Release the pointer
            element.ReleasePointerCapture(args.Pointer);
            nameTextBlock.Text = ((FrameworkElement)sender).Name;

            --numPointers;
        }

        // Route the pointer canceled event to the gesture recognizer.
        // The points are in the reference frame of the canvas that contains the rectangle element.
        void OnPointerCanceled(object sender, PointerRoutedEventArgs args)
        {
            recognizer.CompleteGesture();
            element.ReleasePointerCapture(args.Pointer);
            nameTextBlock.Text = ((FrameworkElement)sender).Name;
            --numPointers;
        }

        // When a manipulation begins, change the color of the object to reflect
        // that a manipulation is in progress
        void OnManipulationStarted(object sender, ManipulationStartedEventArgs e)
        {
            //Border b = element as Border;
            //b.Background = new SolidColorBrush(Windows.UI.Colors.DeepSkyBlue);
        }

        // Process the change resulting from a manipulation
        void OnManipulationUpdated(object sender, ManipulationUpdatedEventArgs e)
        {
            Debug.WriteLine("{0} - {1}", cumulativeTransform.Value.OffsetY, element.ScrollableHeight);
                previousTransform.Matrix = cumulativeTransform.Value;

                // Get the center point of the manipulation for rotation
                Point center = new Point(e.Position.X, e.Position.Y);
                deltaTransform.CenterX = center.X;
                deltaTransform.CenterY = center.Y;

                // Look at the Delta property of the ManipulationDeltaRoutedEventArgs to retrieve
                // the rotation, X, and Y changes
                deltaTransform.Rotation = e.Delta.Rotation;

                deltaTransform.TranslateX = e.Delta.Translation.X;
                deltaTransform.TranslateY = e.Delta.Translation.Y;

                element.ChangeView(null, -cumulativeTransform.Value.OffsetY, null);

            string text = "";

            if (e.Delta.Scale != 1)
                text = text + "Pinching ";
            else if (e.Delta.Translation.X != 0 || e.Delta.Translation.Y != 0)
                text = text + "Scrolling ";

            status = text;
        }

        // When a manipulation that's a result of inertia begins, change the color of the
        // the object to reflect that inertia has taken over
        void OnManipulationInertiaStarting(object sender, ManipulationInertiaStartingEventArgs e)
        {
            //Border b = element as Border;
            //b.Background = new SolidColorBrush(Windows.UI.Colors.RoyalBlue);
        }

        // When a manipulation has finished, reset the color of the object
        void OnManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            //Border b = element as Border;
            //b.Background = new SolidColorBrush(Windows.UI.Colors.LightGray);

            numPointers = 0;
            //statusText.Text = "Move/Rotate";
        }

        private void OnCrossSliding(GestureRecognizer sender, CrossSlidingEventArgs args)
        {
            status = "Swipe";
        }


        private void OnTapped(GestureRecognizer sender, TappedEventArgs args)
        {
            if (args.TapCount <= 1)
                status = "Tapped";
            else
                status = "Double Tapped";
        }

        private void OnRightTapped(GestureRecognizer sender, RightTappedEventArgs args)
        {
            status = "Right Tapped";
        }

        private void OnHolding(GestureRecognizer sender, HoldingEventArgs args)
        {
            status = "Holding";
        }

        private void OnDragging(GestureRecognizer sender, DraggingEventArgs args)
        {
            status = "Dragging";
        }

        // Modify the GestureSettings property to only allow movement on the X axis
        public void LockToXAxis()
        {
            recognizer.CompleteGesture();
            recognizer.GestureSettings |= GestureSettings.ManipulationTranslateY | GestureSettings.ManipulationTranslateX;
            recognizer.GestureSettings ^= GestureSettings.ManipulationTranslateY;
        }

        // Modify the GestureSettings property to only allow movement on the Y axis
        public void LockToYAxis()
        {
            recognizer.CompleteGesture();
            recognizer.GestureSettings |= GestureSettings.ManipulationTranslateY | GestureSettings.ManipulationTranslateX;
            recognizer.GestureSettings ^= GestureSettings.ManipulationTranslateX;
        }

        // Modify the GestureSettings property to allow movement on both the the X and Y axes
        public void MoveOnXAndYAxes()
        {
            recognizer.CompleteGesture();
            recognizer.GestureSettings |= GestureSettings.ManipulationTranslateX | GestureSettings.ManipulationTranslateY;
        }

        // Modify the GestureSettings property to enable or disable inertia based on the passed-in value
        public void UseInertia(bool inertia)
        {
            if (!inertia)
            {
                recognizer.CompleteGesture();
                recognizer.GestureSettings ^= GestureSettings.ManipulationTranslateInertia | GestureSettings.ManipulationRotateInertia;
            }
            else
            {
                recognizer.GestureSettings |= GestureSettings.ManipulationTranslateInertia | GestureSettings.ManipulationRotateInertia;
            }
        }

        public void Reset()
        {
            element.RenderTransform = null;
            recognizer.CompleteGesture();
            InitializeTransforms();
            recognizer.GestureSettings = GenerateDefaultSettings();
        }
    }
}
