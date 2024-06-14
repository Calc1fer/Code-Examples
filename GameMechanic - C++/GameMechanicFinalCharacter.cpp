// Copyright Epic Games, Inc. All Rights Reserved.

#include "GameMechanicFinalCharacter.h"
#include "Camera/CameraComponent.h"
#include "Components/CapsuleComponent.h"
#include "Components/InputComponent.h"
#include "GameFramework/CharacterMovementComponent.h"
#include "GameFramework/Controller.h"
#include "GameFramework/SpringArmComponent.h"

//////////////////////////////////////////////////////////////////////////
// AGameMechanicFinalCharacter

AGameMechanicFinalCharacter::AGameMechanicFinalCharacter()
{
	// Set size for collision capsule
	GetCapsuleComponent()->InitCapsuleSize(42.f, 96.0f);

	// set our turn rate for input
	TurnRateGamepad = 50.f;

	// Don't rotate when the controller rotates. Let that just affect the camera.
	bUseControllerRotationPitch = false;
	bUseControllerRotationYaw = false;
	bUseControllerRotationRoll = false;

	// Configure character movement
	GetCharacterMovement()->bOrientRotationToMovement = true; // Character moves in the direction of input...	
	GetCharacterMovement()->RotationRate = FRotator(0.0f, 500.0f, 0.0f); // ...at this rotation rate

	// Note: For faster iteration times these variables, and many more, can be tweaked in the Character Blueprint
	// instead of recompiling to adjust them
	GetCharacterMovement()->JumpZVelocity = 700.f;
	GetCharacterMovement()->AirControl = 0.35f;
	GetCharacterMovement()->MaxWalkSpeed = 500.f;
	GetCharacterMovement()->MinAnalogWalkSpeed = 20.f;
	GetCharacterMovement()->BrakingDecelerationWalking = 2000.f;

	// Create a camera boom (pulls in towards the player if there is a collision)
	CameraBoom = CreateDefaultSubobject<USpringArmComponent>(TEXT("CameraBoom"));
	CameraBoom->SetupAttachment(RootComponent);
	CameraBoom->TargetArmLength = 500.0f; // The camera follows at this distance behind the character
	CameraBoom->bUsePawnControlRotation = true; // Rotate the arm based on the controller

	// Create a follow camera
	FollowCamera = CreateDefaultSubobject<UCameraComponent>(TEXT("FollowCamera"));
	FollowCamera->SetupAttachment(CameraBoom, USpringArmComponent::SocketName); // Attach the camera to the end of the boom and let the boom adjust to match the controller orientation
	FollowCamera->bUsePawnControlRotation = false; // Camera does not rotate relative to arm

	// Note: The skeletal mesh and anim blueprint references on the Mesh component (inherited from Character) 
	// are set in the derived blueprint asset named ThirdPersonCharacter (to avoid direct content references in C++)
}

//////////////////////////////////////////////////////////////////////////
// Input

void AGameMechanicFinalCharacter::SetupPlayerInputComponent(class UInputComponent* PlayerInputComponent)
{
	// Set up gameplay key bindings
	check(PlayerInputComponent);
	PlayerInputComponent->BindAction("Jump", IE_Pressed, this, &ACharacter::Jump);
	PlayerInputComponent->BindAction("Jump", IE_Released, this, &ACharacter::StopJumping);
	PlayerInputComponent->BindAction("Sprint", IE_Pressed, this, &AGameMechanicFinalCharacter::StartSprint);
	PlayerInputComponent->BindAction("Sprint", IE_Released, this, &AGameMechanicFinalCharacter::StopSprint);
	PlayerInputComponent->BindAction("Throw", IE_Pressed, this, &AGameMechanicFinalCharacter::Throw);

	PlayerInputComponent->BindAxis("Move Forward / Backward", this, &AGameMechanicFinalCharacter::MoveForward);
	PlayerInputComponent->BindAxis("Move Right / Left", this, &AGameMechanicFinalCharacter::MoveRight);

	// We have 2 versions of the rotation bindings to handle different kinds of devices differently
	// "turn" handles devices that provide an absolute delta, such as a mouse.
	// "turnrate" is for devices that we choose to treat as a rate of change, such as an analog joystick
	PlayerInputComponent->BindAxis("Turn Right / Left Mouse", this, &APawn::AddControllerYawInput);
	PlayerInputComponent->BindAxis("Turn Right / Left Gamepad", this, &AGameMechanicFinalCharacter::TurnAtRate);
	PlayerInputComponent->BindAxis("Look Up / Down Mouse", this, &APawn::AddControllerPitchInput);
	PlayerInputComponent->BindAxis("Look Up / Down Gamepad", this, &AGameMechanicFinalCharacter::LookUpAtRate);

	// handle touch devices
	PlayerInputComponent->BindTouch(IE_Pressed, this, &AGameMechanicFinalCharacter::TouchStarted);
	PlayerInputComponent->BindTouch(IE_Released, this, &AGameMechanicFinalCharacter::TouchStopped);

	//FRotator cam_rotation = FRotator(0.f, 105.f, 0.f);
	//CameraBoom->AddRelativeRotation(cam_rotation);
	CameraBoom->SetRelativeLocation(FVector(0.f, 0.f, 75.f));
}

void AGameMechanicFinalCharacter::Tick(float DeltaTime)
{
	FTimerHandle t;
	float duration = 1.f;
	//Use the timer to increase the player health to maximum (cool down period)
	GetWorld()->GetTimerManager().SetTimer(t, this, &AGameMechanicFinalCharacter::increaseHealth, duration, false);
	GEngine->AddOnScreenDebugMessage(-1, 15.0f, FColor::Yellow,
		TEXT("Calling increase the fuckin health please!!!!"));
}

void AGameMechanicFinalCharacter::TouchStarted(ETouchIndex::Type FingerIndex, FVector Location)
{
	Jump();
}

void AGameMechanicFinalCharacter::TouchStopped(ETouchIndex::Type FingerIndex, FVector Location)
{
	StopJumping();
}

void AGameMechanicFinalCharacter::TurnAtRate(float Rate)
{
	// calculate delta for this frame from the rate information
	AddControllerYawInput(Rate * TurnRateGamepad * GetWorld()->GetDeltaSeconds());
}

void AGameMechanicFinalCharacter::LookUpAtRate(float Rate)
{
	// calculate delta for this frame from the rate information
	AddControllerPitchInput(Rate * TurnRateGamepad * GetWorld()->GetDeltaSeconds());
}

void AGameMechanicFinalCharacter::MoveForward(float Value)
{
	if ((Controller != nullptr) && (Value != 0.0f))
	{
		// find out which way is forward
		const FRotator Rotation = Controller->GetControlRotation();
		const FRotator YawRotation(0, Rotation.Yaw, 0);

		// get forward vector
		const FVector Direction = FRotationMatrix(YawRotation).GetUnitAxis(EAxis::X);
		AddMovementInput(Direction, Value);
	}
}

void AGameMechanicFinalCharacter::MoveRight(float Value)
{
	if ( (Controller != nullptr) && (Value != 0.0f) )
	{
		// find out which way is right
		const FRotator Rotation = Controller->GetControlRotation();
		const FRotator YawRotation(0, Rotation.Yaw, 0);
	
		// get right vector 
		const FVector Direction = FRotationMatrix(YawRotation).GetUnitAxis(EAxis::Y);
		// add movement in that direction
		AddMovementInput(Direction, Value);
	}
}

void AGameMechanicFinalCharacter::StartSprint()
{
	if ((Controller != nullptr))
	{
		//Logic for sprinting
		//Set the max speed to 600f

		//Get a ref to the character movement and set the max walking speed to the max_speed
		move_comp = GetCharacterMovement();
		move_comp->MaxWalkSpeed = max_speed;
	}

}

void AGameMechanicFinalCharacter::StopSprint()
{
	if ((Controller != nullptr))
	{
		//Logic for stopping the sprint
		//Set the max speed to 400f
		move_comp = GetCharacterMovement();
		move_comp->MaxWalkSpeed = min_speed;
	}
}

void AGameMechanicFinalCharacter::Throw()
{
	//No throwing when falling
	if (GetCharacterMovement()->IsFalling())
	{
		return;
	}
	else
	{
		if ((Controller != nullptr) && (no_movement == false) && (health > -0.1))
		{
			//Delay the player spawning a projectile until the animation reaches here
			if (action == true)
			{
				FTimerHandle t1;
				float duration1 = 0.7f;

				//Do this section once! Don't want spamming
				//get the animation montage to play when throwing
				throw_anim = GetMesh()->GetAnimInstance();

				//play the montage
				throw_anim->Montage_Play(throw_m);
				action = false;

				//Logic for the throwing ability
				//get ref to the player char and stop them from moving when throwing
				move_comp = GetCharacterMovement();
				move_comp->StopMovementImmediately();
				move_comp->DisableMovement();
				setHeal(true);

				//When animation gets to 0.7 seconds, spawn the projectile
				GetWorld()->GetTimerManager().SetTimer(t1, this, &AGameMechanicFinalCharacter::throwAction, duration1, false);
				reduceHealth();
			}

			no_movement = true;
			//Declare variable to time how long the player cannot move for
			FTimerHandle t;
			//get the duration of the montage - the a delay before spawning the projectile
			//and this is how long to continue to prevent player movement
			float duration = throw_m->GetPlayLength() - 0.3;

			//Use the timer to prevent player movement
			GetWorld()->GetTimerManager().SetTimer(t, this, &AGameMechanicFinalCharacter::StopThrow, duration, false);
		}
	}
}

void AGameMechanicFinalCharacter::StopThrow()
{
	if ((Controller != nullptr) && no_movement == true)
	{
		//Logic for stopping the throw ability
		//Set the movement back to walking
		move_comp = GetCharacterMovement();
		move_comp->SetMovementMode(EMovementMode::MOVE_Walking);
		no_movement = false;
		UE_LOG(LogTemp, Error, TEXT("Countdown has expired!"));
		action = true;
	}
}

void AGameMechanicFinalCharacter::throwAction()
{
	FVector cam_location;
	FRotator cam_rotation;
	Controller->GetPlayerViewPoint(cam_location, cam_rotation);
	FVector new_location = FVector(cam_location.X, cam_location.Y - 30, cam_location.Z);

	float magnitude = 100000.f;

	FVector force = GetActorForwardVector() * magnitude;
	//offset the camera so it spawns the actor at another location
	cam_location += cam_rotation.Vector() * 600.f;

	AAThrowable* new_throwable = GetWorld()->SpawnActor<AAThrowable>(AAThrowable::StaticClass(), cam_location, cam_rotation);

	//Add impulse to the throwable object
	if (new_throwable)
	{
		UE_LOG(LogTemp, Warning, TEXT("Throwable Actor Spawned at Location: %s and Rotation: %s"), *cam_location.ToString(), *cam_rotation.ToString());

		UStaticMeshComponent* throwable_mesh = new_throwable->GetMeshComp();

		if (throwable_mesh)
		{
			UE_LOG(LogTemp, Warning, TEXT("Throwable Mesh Component is valid"));
			throwable_mesh->AddImpulse(force);
		}
		else
		{
			UE_LOG(LogTemp, Warning, TEXT("Throwable Mesh Component is not valid"));
		}
	}
}

float AGameMechanicFinalCharacter::GetHealth()
{
	return health;
}

void AGameMechanicFinalCharacter::reduceHealth()
{
	health = health - damage;
}

void AGameMechanicFinalCharacter::increaseHealth()
{
	float max_health = 100.f;

	if (health < max_health)
	{
		if (health > 0)
		{
			health = health + 0.05f;
		}
	}
	
	if(health < 0)
	{
		health = 0.f;
		GEngine->AddOnScreenDebugMessage(-1, 15.0f, FColor::Yellow,
			TEXT("Calling exhaustion"));
		exhaustion();
	}

	FString health_str = FString::SanitizeFloat(health);
	GEngine->AddOnScreenDebugMessage(-1, 15.0f, FColor::Yellow, health_str);
}

void AGameMechanicFinalCharacter::setHeal(bool toggle)
{
	heal = toggle;
}

void AGameMechanicFinalCharacter::exhaustion()
{
	//Code here for playing a montage of the player being exhausted for a set time before regenerating health
	exhaust_anim = GetMesh()->GetAnimInstance();

	//player the exhausted montage
	exhaust_anim->Montage_Play(exhaust_m);

	//disable movement etc
	move_comp = GetCharacterMovement();
	move_comp->StopMovementImmediately();
	move_comp->DisableMovement();

	//Delay the player before healing and activating movement
	FTimerHandle t;
	float duration = exhaust_m->GetPlayLength();

	//remember to set so the player health begins regenerating again or we will be stuck in a loop here
	health = 1.f;

	//Use the timer to increase the player health to maximum (cool down period)
	GetWorld()->GetTimerManager().SetTimer(t, this, &AGameMechanicFinalCharacter::increaseHealth, duration, false);
}