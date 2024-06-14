// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Character.h"
#include "Components/StaticMeshComponent.h"
#include "Particles/ParticleSystemComponent.h"
#include "Particles/ParticleSystem.h"
#include "Components/CapsuleComponent.h"
#include "Kismet/GameplayStatics.h"
#include "Components/SkeletalMeshComponent.h"
#include "DummyEnemy.generated.h"

UCLASS()
class GAMEMECHANICFINAL_API ADummyEnemy : public ACharacter
{
	GENERATED_BODY()

public:
	// Sets default values for this character's properties
	ADummyEnemy();

	UFUNCTION()
		void OnHit(UPrimitiveComponent* HitComponent, AActor* OtherActor, UPrimitiveComponent* OtherComp,
			FVector NormalImpulse, const FHitResult& Hit);
	void DestroyActor();

protected:
	// Called when the game starts or when spawned
	virtual void BeginPlay() override;

	//Variables go here
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Mesh")
		USkeletalMeshComponent* mesh_comp;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Assets")
		UStaticMeshComponent* hit_box;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Assets")
		UParticleSystemComponent* particle_sys;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Variables")
		bool is_dead = false;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Variables")
		float damage = 50.f;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Variables")
		float health = 100.f;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = Animations)
		UAnimMontage* idle_m;
		
	UAnimInstance* idle_anim;
public:	
	// Called every frame
	virtual void Tick(float DeltaTime) override;

	// Called to bind functionality to input
	virtual void SetupPlayerInputComponent(class UInputComponent* PlayerInputComponent) override;

};
