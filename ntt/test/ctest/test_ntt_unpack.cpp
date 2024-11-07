/* Copyright 2019-2024 Canaan Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
#include "ntt_test.h"
#include "ortki_helper.h"
#include <gtest/gtest.h>
#include <iostream>
#include <nncase/ntt/ntt.h>
#include <ortki/operators.h>

using namespace nncase;
using namespace ortki;

TEST(UnpackTestFloat, fixed_shape_dim0) {
    constexpr size_t P = NTT_VLEN / (sizeof(float) * 8);
    constexpr size_t M = P * 32;
    constexpr size_t N = 1024;
    float min_input = -10.0f;
    float max_input = 10.0f;

    using tensor_type1 =
        ntt::tensor<ntt::vector<float, P>, ntt::fixed_shape<M / P, N>>;
    using tensor_type2 = ntt::tensor<float, ntt::fixed_shape<M, N>>;

    // init
    std::unique_ptr<tensor_type1> ntt_input(new tensor_type1);
    NttTest::init_tensor(*ntt_input, min_input, max_input);

    // ntt
    std::unique_ptr<tensor_type2> ntt_output1(new tensor_type2);
    ntt::unpack<0>(*ntt_input, *ntt_output1);

    // ort
    auto ort_input = NttTest::ntt2ort(*ntt_input);
    int64_t perms[] = {0, 2, 1};
    auto tmp = ortki_Transpose(ort_input, perms, std::size(perms));
    int64_t data[] = {M, N};
    int64_t data_shape[] = {std::size(data)};
    auto ort_type = NttTest::primitive_type2ort_type<int64_t>();
    auto shape = make_tensor(reinterpret_cast<void *>(data), ort_type,
                                  data_shape, std::size(data_shape));
    auto ort_output = ortki_Reshape(tmp, shape, 0);

    // compare
    std::unique_ptr<tensor_type2> ntt_output2(new tensor_type2);
    NttTest::ort2ntt(ort_output, *ntt_output2);
    EXPECT_TRUE(NttTest::compare_tensor(*ntt_output1, *ntt_output2));
}

TEST(UnpackTestFloat, fixed_shape_dim1) {
    constexpr size_t P = NTT_VLEN / (sizeof(float) * 8);
    constexpr size_t M = 1024;
    constexpr size_t N = P * 32;
    float min_input = -10.0f;
    float max_input = 10.0f;

    using tensor_type1 =
        ntt::tensor<ntt::vector<float, P>, ntt::fixed_shape<M, N / P>>;
    using tensor_type2 = ntt::tensor<float, ntt::fixed_shape<M, N>>;

    // init
    std::unique_ptr<tensor_type1> ntt_input(new tensor_type1);
    NttTest::init_tensor(*ntt_input, min_input, max_input);

    // ntt
    std::unique_ptr<tensor_type2> ntt_output1(new tensor_type2);
    ntt::unpack<1>(*ntt_input, *ntt_output1);

    // ort
    auto ort_input = NttTest::ntt2ort(*ntt_input);
    int64_t data[] = {M, N};
    int64_t data_shape[] = {std::size(data)};
    auto ort_type = NttTest::primitive_type2ort_type<int64_t>();
    auto shape = make_tensor(reinterpret_cast<void *>(data), ort_type,
                                  data_shape, std::size(data_shape));
    auto ort_output = ortki_Reshape(ort_input, shape, 0);

    // compare
    std::unique_ptr<tensor_type2> ntt_output2(new tensor_type2);
    NttTest::ort2ntt(ort_output, *ntt_output2);
    EXPECT_TRUE(NttTest::compare_tensor(*ntt_output1, *ntt_output2));
}

TEST(UnpackTestFloat, fixed_shape_dim0_1) {
    constexpr size_t P = NTT_VLEN / (sizeof(float) * 8);
    constexpr size_t M = P * 32;
    constexpr size_t N = P * 32;
    float min_input = -10.0f;
    float max_input = 10.0f;

    // init
    using tensor_type1 =
        ntt::tensor<ntt::vector<float, P, P>, ntt::fixed_shape<M / P, N / P>>;
    std::unique_ptr<tensor_type1> ntt_input(new tensor_type1);
    NttTest::init_tensor(*ntt_input, min_input, max_input);

    // ntt
    using tensor_type2 = ntt::tensor<float, ntt::fixed_shape<M, N>>;
    std::unique_ptr<tensor_type2> ntt_output1(new tensor_type2);
    ntt::unpack<0, 1>(*ntt_input, *ntt_output1);

    // ort
    auto ort_input = NttTest::ntt2ort(*ntt_input);
    int64_t perms[] = {0, 2, 1, 3};
    auto tmp = ortki_Transpose(ort_input, perms, std::size(perms));
    int64_t data[] = {M, N};
    int64_t data_shape[] = {std::size(data)};
    auto ort_type = NttTest::primitive_type2ort_type<int64_t>();
    auto shape = make_tensor(reinterpret_cast<void *>(data), ort_type,
                                  data_shape, std::size(data_shape));
    auto ort_output = ortki_Reshape(tmp, shape, 0);

    // compare
    std::unique_ptr<tensor_type2> ntt_output2(new tensor_type2);
    NttTest::ort2ntt(ort_output, *ntt_output2);
    EXPECT_TRUE(NttTest::compare_tensor(*ntt_output1, *ntt_output2));
}

TEST(UnpackTestFloat, ranked_shape_dim0) {
    constexpr size_t P = NTT_VLEN / (sizeof(float) * 8);
    constexpr size_t M = P * 32;
    constexpr size_t N = 1024;
    float min_input = -10.0f;
    float max_input = 10.0f;

    // init
    using tensor_type1 =
        ntt::tensor<ntt::vector<float, P>, ntt::ranked_shape<2>>;
    auto shape1 = ntt::make_ranked_shape(M / P, N);
    std::unique_ptr<tensor_type1> ntt_input(new tensor_type1(shape1));
    NttTest::init_tensor(*ntt_input, min_input, max_input);

    // ntt
    using tensor_type2 = ntt::tensor<float, ntt::ranked_shape<2>>;
    auto shape2 = ntt::make_ranked_shape(M, N);
    std::unique_ptr<tensor_type2> ntt_output1(new tensor_type2(shape2));
    ntt::unpack<0>(*ntt_input, *ntt_output1);

    // ort
    auto ort_input = NttTest::ntt2ort(*ntt_input);
    int64_t perms[] = {0, 2, 1};
    auto tmp = ortki_Transpose(ort_input, perms, std::size(perms));
    int64_t data[] = {M, N};
    int64_t data_shape[] = {std::size(data)};
    auto ort_type = NttTest::primitive_type2ort_type<int64_t>();
    auto shape = make_tensor(reinterpret_cast<void *>(data), ort_type,
                                  data_shape, std::size(data_shape));
    auto ort_output = ortki_Reshape(tmp, shape, 0);

    // compare
    std::unique_ptr<tensor_type2> ntt_output2(new tensor_type2(shape2));
    NttTest::ort2ntt(ort_output, *ntt_output2);
    EXPECT_TRUE(NttTest::compare_tensor(*ntt_output1, *ntt_output2));
}

TEST(UnpackTestFloat, ranked_shape_dim1) {
    constexpr size_t P = NTT_VLEN / (sizeof(float) * 8);
    constexpr size_t M = 1024;
    constexpr size_t N = P * 32;
    float min_input = -10.0f;
    float max_input = 10.0f;

    // init
    using tensor_type1 =
        ntt::tensor<ntt::vector<float, P>, ntt::ranked_shape<2>>;
    auto shape1 = ntt::make_ranked_shape(M, N / P);
    std::unique_ptr<tensor_type1> ntt_input(new tensor_type1(shape1));
    NttTest::init_tensor(*ntt_input, min_input, max_input);

    // ntt
    using tensor_type2 = ntt::tensor<float, ntt::ranked_shape<2>>;
    auto shape2 = ntt::make_ranked_shape(M, N);
    std::unique_ptr<tensor_type2> ntt_output1(new tensor_type2(shape2));
    ntt::unpack<1>(*ntt_input, *ntt_output1);

    // ort
    auto ort_input = NttTest::ntt2ort(*ntt_input);
    int64_t data[] = {M, N};
    int64_t data_shape[] = {std::size(data)};
    auto ort_type = NttTest::primitive_type2ort_type<int64_t>();
    auto shape = make_tensor(reinterpret_cast<void *>(data), ort_type,
                                  data_shape, std::size(data_shape));
    auto ort_output = ortki_Reshape(ort_input, shape, 0);

    // compare
    std::unique_ptr<tensor_type2> ntt_output2(new tensor_type2(shape2));
    NttTest::ort2ntt(ort_output, *ntt_output2);
    EXPECT_TRUE(NttTest::compare_tensor(*ntt_output1, *ntt_output2));
}

TEST(UnpackTestFloat, ranked_shape_dim0_1) {
    constexpr size_t P = NTT_VLEN / (sizeof(float) * 8);
    constexpr size_t M = P * 32;
    constexpr size_t N = P * 32;
    float min_input = -10.0f;
    float max_input = 10.0f;

    // init
    using tensor_type1 =
        ntt::tensor<ntt::vector<float, P, P>, ntt::ranked_shape<2>>;
    auto shape1 = ntt::make_ranked_shape(M / P, N / P);
    std::unique_ptr<tensor_type1> ntt_input(new tensor_type1(shape1));
    NttTest::init_tensor(*ntt_input, min_input, max_input);

    // ntt
    using tensor_type2 = ntt::tensor<float, ntt::ranked_shape<2>>;
    auto shape2 = ntt::make_ranked_shape(M, N);
    std::unique_ptr<tensor_type2> ntt_output1(new tensor_type2(shape2));
    ntt::unpack<0, 1>(*ntt_input, *ntt_output1);

    // ort
    auto ort_input = NttTest::ntt2ort(*ntt_input);
    int64_t perms[] = {0, 2, 1, 3};
    auto tmp = ortki_Transpose(ort_input, perms, std::size(perms));
    int64_t data[] = {M, N};
    int64_t data_shape[] = {std::size(data)};
    auto ort_type = NttTest::primitive_type2ort_type<int64_t>();
    auto shape = make_tensor(reinterpret_cast<void *>(data), ort_type,
                                  data_shape, std::size(data_shape));
    auto ort_output = ortki_Reshape(tmp, shape, 0);

    // compare
    std::unique_ptr<tensor_type2> ntt_output2(new tensor_type2(shape2));
    NttTest::ort2ntt(ort_output, *ntt_output2);
    EXPECT_TRUE(NttTest::compare_tensor(*ntt_output1, *ntt_output2));
}

int main(int argc, char *argv[]) {
    ::testing::InitGoogleTest(&argc, argv);
    return RUN_ALL_TESTS();
}