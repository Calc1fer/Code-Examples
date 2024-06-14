// Copyright Epic Games, Inc. All Rights Reserved.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Character.h"
#include "AThrowable.h"
#include "Engine/World.h"
#include "GameMechanicFinalCharacter.generated.h"

UCLASS(config=Game)
class AGameMechanicFinalCharacter : public ACharacter
{
	GENERATED_BODY()

	/** Camera boom positioning the camera behind the character */
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = Camera, meta = (AllowPrivateAccess = "true"))
	class USpringArmComponent* CameraBoom;

	/** Follow camera */
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = Camera, meta = (AllowPrivateAccess = "true"))
	class UCameraComponent* FollowCamera;
public:
	AGameMechanicFinalCharacter();

	virtual void Tick(float DeltaTime) override;
	float GetHealth();

	/** Base turn rate, in deg/sec. Other scaling may affect final turn rate. */
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category=Input)
	float TurnRateGamepad;

protected:

	/** Called for forwards/backward input */
	void MoveForward(float Value);

	/** Called for side to side input */
	void MoveRight(float Value);

	/*Cal script starts*/
	void StartSprint();
	void StopSprint();
	void Throw();
	void StopThrow();
	void throwAction();
	void reduceHealth();
	void increaseHealth();
	void setHeal(bool toggle);
	void stopHeal();
	void exhaustion();
	/*Cal script ends*/

	/** 
	 * Called via input to turn at a given rate. 
	 * @param Rate	This is a normalized rate, i.e. 1.0 means 100% of desired turn rate
	 */
	void TurnAtRate(float Rate);

	/**
	 * Called via input to turn look up/down at a given rate. 
	 * @param Rate	This is a normalized rate, i.e. 1.0 means 100% of desired turn rate
	 */
	void LookUpAtRate(float Rate);

	/** Handler for when a touch input begins. */
	void TouchStarted(ETouchIndex::Type FingerIndex, FVector Location);

	/** Handler for when a touch input stops. */
	void TouchStopped(ETouchIndex::Type FingerIndex, FVector Location);

protected:
	// APawn interface
	virtual void SetupPlayerInputComponent(class UInputComponent* PlayerInputComponent) override;
	// End of APawn interface

	/*Cal script starts*/
	//Variables
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Variables")
		float max_speed;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Variables")
		float min_speed;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Animations")
		UAnimMontage* throw_m;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Animations")
		UAnimMontage* exhaust_m;
	UPROPERTY(EditAnywhere)
		bool throwing;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Assets")
		AAThrowable* throwable;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Variables")
		float health = 100.f;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Variables")
		float damage = 50.f;

	UCharacterMovementComponent* move_comp;
	UAnimInstance* throw_anim;
	UAnimInstance* exhaust_anim;

	bool no_movement = false;
	bool action = true;
	bool is_jumping = false;
	bool heal = false;
	/*Cal script ends*/

public:
	/** Returns CameraBoom subobject **/
	FORCEINLINE class USpringArmComponent* GetCameraBoom() const { return CameraBoom; }
	/** Returns FollowCamera subobject **/
	FORCEINLINE class UCameraComponent* GetFollowCamera() const { return FollowCamera; }
};

