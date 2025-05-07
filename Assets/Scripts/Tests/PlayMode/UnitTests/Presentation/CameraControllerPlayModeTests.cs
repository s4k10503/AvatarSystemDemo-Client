using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

using Presentation.View;

public class CameraControllerPlayModeTests
{
    private GameObject cameraGo;
    private Camera cameraComponent; // カメラコンポーネントを保持
    private CameraView cameraView;
    private GameObject targetGo;
    private Transform targetTransform;

    private const float Tolerance = 0.01f; // float比較用の許容誤差

    // 各テストの前に実行されるセットアップ処理
    [UnitySetUp]
    public IEnumerator UnitySetUp()
    {
        // --- カメラオブジェクトの生成と設定 ---
        cameraGo = new GameObject("TestCamera");
        // Camera コンポーネントがないと LookAt が正しく動作しない可能性があるため追加
        cameraComponent = cameraGo.AddComponent<Camera>();
        // CameraView を追加
        cameraView = cameraGo.AddComponent<CameraView>();
        // カメラの初期位置を設定 (例: ターゲットから少し離れた位置)
        cameraGo.transform.position = new Vector3(0, 2, -5);
        cameraGo.transform.rotation = Quaternion.identity; // 初期回転をリセット

        // --- ターゲットオブジェクトの生成と設定 ---
        targetGo = GameObject.CreatePrimitive(PrimitiveType.Cube); // シンプルなCubeをターゲットにする
        targetGo.name = "TestTarget";
        targetTransform = targetGo.transform;
        targetTransform.position = Vector3.zero; // 原点に配置

        // オブジェクトの生成とコンポーネントの追加が完了するのを待つ
        yield return null;
    }

    // 各テストの後に実行される後片付け処理
    [TearDown]
    public void TearDown()
    {
        // テスト中に生成したGameObjectを破棄
        if (cameraGo != null)
        {
            Object.Destroy(cameraGo);
        }
        if (targetGo != null)
        {
            Object.Destroy(targetGo);
        }
    }

    /// <summary>
    /// SetTargetメソッドが正しく初期化を行い、カメラがターゲットを正しく設定することを確認するテスト
    /// </summary>
    [UnityTest]
    public IEnumerator SetTarget_InitializesCamera()
    {
        // Arrange
        Vector3 initialCameraPos = cameraGo.transform.position;

        // Act
        cameraView.SetTarget(targetTransform);
        // SetTarget内の計算が完了するのを待つかもしれないので1フレーム待機
        yield return null;

        // Assert
        // SetTarget 呼び出しでカメラの位置が変わらないことを確認
        Assert.AreEqual(initialCameraPos, cameraGo.transform.position, "Camera position should not change on SetTarget.");

        // ターゲットを設定すると初期化が完了していることを確認（回転角度を設定してカメラが動くことで検証）
        cameraView.SetRotationAngle(10f); // 絶対角度10度に設定
        yield return null;

        // 回転後にカメラが移動していることを確認（ターゲットが設定されている証拠）
        Assert.AreNotEqual(initialCameraPos, cameraGo.transform.position, "Camera should move after rotation when target is set.");
    }

    /// <summary>
    /// ターゲットが設定されていない場合、SetRotationAngleが何もしないことを確認するテスト
    /// </summary>
    [UnityTest]
    public IEnumerator UpdateRotation_NoTarget_DoesNothing()
    {
        // Arrange
        Vector3 initialPos = cameraGo.transform.position;
        Quaternion initialRot = cameraGo.transform.rotation;
        // SetTarget を呼び出さない状態にする

        // Act
        // 回転角度を設定
        cameraView.SetRotationAngle(10f);
        yield return null; // フレームを進める

        // Assert
        // 位置と回転が変わっていないことを確認
        Assert.AreEqual(initialPos, cameraGo.transform.position, "Position should not change when target is null.");
        Assert.AreEqual(initialRot, cameraGo.transform.rotation, "Rotation should not change when target is null.");
    }

    /// <summary>
    /// 正の回転角度でカメラが正しく回転することを確認するテスト
    /// </summary>
    [UnityTest]
    public IEnumerator UpdateRotation_RotatesCameraCorrectly_PositiveX()
    {
        // Arrange
        cameraView.SetTarget(targetTransform);
        yield return null; // SetTargetの反映を待つ
        Vector3 initialPos = cameraGo.transform.position;
        float initialY = initialPos.y;
        // XZ平面での初期距離を計算
        float initialXZDistance = Vector3.Distance(new Vector3(initialPos.x, 0, initialPos.z), new Vector3(targetTransform.position.x, 0, targetTransform.position.z));

        // Act
        // 累積的に回転角度を増やしていく
        float totalRotation = 0f;
        const int framesToSimulate = 10; // シミュレーションするフレーム数
        const float rotationPerFrame = 5.0f; // 1フレームあたりの回転角度

        for (int i = 0; i < framesToSimulate; i++)
        {
            totalRotation += rotationPerFrame;
            cameraView.SetRotationAngle(totalRotation);
            yield return null; // フレームを進める
        }
        Vector3 finalPos = cameraGo.transform.position;

        // Assert
        // 1. 位置が変わっていること（回転したこと）
        Assert.AreNotEqual(initialPos, finalPos, "Position should change after rotation.");

        // 2. Y座標が維持されていること
        Assert.That(finalPos.y, Is.EqualTo(initialY).Within(Tolerance), $"Y position should remain at {initialY}.");

        // 3. XZ平面上のターゲットからの距離が維持されていること
        float finalXZDistance = Vector3.Distance(new Vector3(finalPos.x, 0, finalPos.z), new Vector3(targetTransform.position.x, 0, targetTransform.position.z));
        Assert.That(finalXZDistance, Is.EqualTo(initialXZDistance).Within(Tolerance), $"XZ distance from target should remain at {initialXZDistance}.");

        // 4. カメラがターゲット（Y軸はカメラの高さに合わせた位置）を向いていること
        Vector3 lookAtTargetPos = new Vector3(targetTransform.position.x, finalPos.y, targetTransform.position.z);
        Vector3 directionToTarget = (lookAtTargetPos - finalPos).normalized;
        Assert.That(Vector3.Dot(cameraGo.transform.forward, directionToTarget), Is.GreaterThan(1.0f - Tolerance),
            "Camera's forward should point towards the adjusted target position.");

        // 5. 回転方向の確認 (初期位置(0,2,-5)でターゲット(0,0,0)の場合、Xが増加しZが0に近づくはず)
        Assert.Greater(finalPos.x, initialPos.x, "Rotation with positive angle should increase X coordinate (given initial pos 0,2,-5).");
        Assert.Greater(finalPos.z, initialPos.z, "Rotation with positive angle should increase Z coordinate (towards 0) (given initial pos 0,2,-5).");
    }

    /// <summary>
    /// 負の回転角度でカメラが逆方向に正しく回転することを確認するテスト
    /// </summary>
    [UnityTest]
    public IEnumerator UpdateRotation_RotatesCameraCorrectly_NegativeX()
    {
        // Arrange (PositiveXと同様)
        cameraView.SetTarget(targetTransform);
        yield return null;
        Vector3 initialPos = cameraGo.transform.position;
        float initialY = initialPos.y;
        float initialXZDistance = Vector3.Distance(new Vector3(initialPos.x, 0, initialPos.z), new Vector3(targetTransform.position.x, 0, targetTransform.position.z));

        // Act
        // 累積的に負の回転角度を増やしていく
        float totalRotation = 0f;
        const int framesToSimulate = 10;
        const float rotationPerFrame = -5.0f; // 負の方向へ回転

        for (int i = 0; i < framesToSimulate; i++)
        {
            totalRotation += rotationPerFrame;
            cameraView.SetRotationAngle(totalRotation);
            yield return null;
        }
        Vector3 finalPos = cameraGo.transform.position;

        // Assert (基本的なチェックはPositiveXと同様)
        Assert.AreNotEqual(initialPos, finalPos);
        Assert.That(finalPos.y, Is.EqualTo(initialY).Within(Tolerance));
        float finalXZDistance = Vector3.Distance(new Vector3(finalPos.x, 0, finalPos.z), new Vector3(targetTransform.position.x, 0, targetTransform.position.z));
        Assert.That(finalXZDistance, Is.EqualTo(initialXZDistance).Within(Tolerance));
        Vector3 lookAtTargetPos = new Vector3(targetTransform.position.x, finalPos.y, targetTransform.position.z);
        Vector3 directionToTarget = (lookAtTargetPos - finalPos).normalized;
        Assert.That(Vector3.Dot(cameraGo.transform.forward, directionToTarget), Is.GreaterThan(1.0f - Tolerance));

        // 5. 回転方向の確認 (初期位置(0,2,-5)の場合、Xが減少しZが0に近づくはず)
        Assert.Less(finalPos.x, initialPos.x, "Rotation with negative angle should decrease X coordinate (given initial pos 0,2,-5).");
        Assert.Greater(finalPos.z, initialPos.z, "Rotation with negative angle should increase Z coordinate (towards 0) (given initial pos 0,2,-5).");
    }

    /// <summary>
    /// Y方向の回転が適用されないことをテスト
    /// </summary>
    [UnityTest]
    public IEnumerator SetFaceMode_SwitchesCameraMode()
    {
        // Arrange
        cameraView.SetTarget(targetTransform);
        yield return null;
        Vector3 initialPos = cameraGo.transform.position;
        Quaternion initialRot = cameraGo.transform.rotation;

        // カメラの位置をいったん動かす
        cameraView.SetRotationAngle(45f);
        yield return null;

        // カメラが実際に動いたことを確認
        Assert.AreNotEqual(initialPos, cameraGo.transform.position, "Camera should have moved after setting rotation angle.");

        // Act - 顔モードに切り替え
        cameraView.SetFaceMode(true);
        yield return null;

        // 顔モードでの位置を保存
        Vector3 faceModePos = cameraGo.transform.position;

        // 体型モードに戻す
        cameraView.SetFaceMode(false);
        yield return null;

        // Assert
        // 顔モードと体型モードで位置が異なることを確認
        Assert.AreNotEqual(faceModePos, cameraGo.transform.position, "Camera position should be different between face mode and body mode.");
    }
}
