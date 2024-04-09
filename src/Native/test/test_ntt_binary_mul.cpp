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
#include <gtest/gtest.h>
#include <iostream>
#include <nncase/ntt/ntt.h>
#include <ortki/operators.h>
#include <string_view>

using namespace nncase;
using namespace ortki;

TEST(BinaryTestMulFloat, fixed_fixed_fixed) {
    // init
    using tensor_type = ntt::tensor<float, ntt::fixed_shape<1, 3, 16, 16>>;
    std::unique_ptr<tensor_type> ntt_lhs(new tensor_type);
    std::unique_ptr<tensor_type> ntt_rhs(new tensor_type);
    NttTest::init_tensor(*ntt_lhs, -10.f, 10.f);
    NttTest::init_tensor(*ntt_rhs, -10.f, 10.f);

    // ntt
    std::unique_ptr<tensor_type> ntt_output1(new tensor_type);
    ntt::binary<ntt::ops::mul>(*ntt_lhs, *ntt_rhs, *ntt_output1);

    // ort
    auto ort_lhs = NttTest::ntt2ort(*ntt_lhs);
    auto ort_rhs = NttTest::ntt2ort(*ntt_rhs);
    auto ort_output = ortki_Mul(ort_lhs, ort_rhs);

    // compare
    std::unique_ptr<tensor_type> ntt_output2(new tensor_type);
    NttTest::ort2ntt(ort_output, *ntt_output2);
    EXPECT_TRUE(NttTest::compare_tensor(*ntt_output1, *ntt_output2));
}

TEST(BinaryTestMulFloat, fixed_fixed_fixed_broadcast_lhs_scalar) {
    // init
    using tensor_type1 = ntt::tensor<float, ntt::fixed_shape<1>>;
    std::unique_ptr<tensor_type1> ntt_rhs(new tensor_type1);
    NttTest::init_tensor(*ntt_rhs, -10.f, 10.f);

    using tensor_type2 = ntt::tensor<float, ntt::fixed_shape<1, 3, 16, 16>>;
    std::unique_ptr<tensor_type2> ntt_lhs(new tensor_type2);
    NttTest::init_tensor(*ntt_lhs, -10.f, 10.f);

    // ntt
    std::unique_ptr<tensor_type2> ntt_output1(new tensor_type2);
    ntt::binary<ntt::ops::mul>(*ntt_lhs, *ntt_rhs, *ntt_output1);

    // ort
    auto ort_lhs = NttTest::ntt2ort(*ntt_lhs);
    auto ort_rhs = NttTest::ntt2ort(*ntt_rhs);
    auto ort_output = ortki_Mul(ort_lhs, ort_rhs);

    // compare
    std::unique_ptr<tensor_type2> ntt_output2(new tensor_type2);
    NttTest::ort2ntt(ort_output, *ntt_output2);
    EXPECT_TRUE(NttTest::compare_tensor(*ntt_output1, *ntt_output2));
}

TEST(BinaryTestMulFloat, fixed_fixed_fixed_broadcast_rhs_scalar) {
    // init
    using tensor_type1 = ntt::tensor<float, ntt::fixed_shape<1, 3, 16, 16>>;
    std::unique_ptr<tensor_type1> ntt_lhs(new tensor_type1);
    NttTest::init_tensor(*ntt_lhs, -10.f, 10.f);

    using tensor_type2 = ntt::tensor<float, ntt::fixed_shape<1>>;
    std::unique_ptr<tensor_type2> ntt_rhs(new tensor_type2);
    NttTest::init_tensor(*ntt_rhs, -10.f, 10.f);

    // ntt
    std::unique_ptr<tensor_type1> ntt_output1(new tensor_type1);
    ntt::binary<ntt::ops::mul>(*ntt_lhs, *ntt_rhs, *ntt_output1);

    // ort
    auto ort_lhs = NttTest::ntt2ort(*ntt_lhs);
    auto ort_rhs = NttTest::ntt2ort(*ntt_rhs);
    auto ort_output = ortki_Mul(ort_lhs, ort_rhs);

    // compare
    std::unique_ptr<tensor_type1> ntt_output2(new tensor_type1);
    NttTest::ort2ntt(ort_output, *ntt_output2);
    EXPECT_TRUE(NttTest::compare_tensor(*ntt_output1, *ntt_output2));
}

TEST(BinaryTestMulFloat, fixed_fixed_fixed_broadcast_lhs_vector) {
    // init
    using tensor_type1 = ntt::tensor<float, ntt::fixed_shape<16>>;
    std::unique_ptr<tensor_type1> ntt_rhs(new tensor_type1);
    NttTest::init_tensor(*ntt_rhs, -10.f, 10.f);

    using tensor_type2 = ntt::tensor<float, ntt::fixed_shape<1, 3, 16, 16>>;
    std::unique_ptr<tensor_type2> ntt_lhs(new tensor_type2);
    NttTest::init_tensor(*ntt_lhs, -10.f, 10.f);

    // ntt
    std::unique_ptr<tensor_type2> ntt_output1(new tensor_type2);
    ntt::binary<ntt::ops::mul>(*ntt_lhs, *ntt_rhs, *ntt_output1);

    // ort
    auto ort_lhs = NttTest::ntt2ort(*ntt_lhs);
    auto ort_rhs = NttTest::ntt2ort(*ntt_rhs);
    auto ort_output = ortki_Mul(ort_lhs, ort_rhs);

    // compare
    std::unique_ptr<tensor_type2> ntt_output2(new tensor_type2);
    NttTest::ort2ntt(ort_output, *ntt_output2);
    EXPECT_TRUE(NttTest::compare_tensor(*ntt_output1, *ntt_output2));
}

TEST(BinaryTestMulFloat, fixed_fixed_fixed_broadcast_rhs_vector) {
    // init
    using tensor_type1 = ntt::tensor<float, ntt::fixed_shape<1, 3, 16, 16>>;
    std::unique_ptr<tensor_type1> ntt_lhs(new tensor_type1);
    NttTest::init_tensor(*ntt_lhs, -10.f, 10.f);

    using tensor_type2 = ntt::tensor<float, ntt::fixed_shape<16>>;
    std::unique_ptr<tensor_type2> ntt_rhs(new tensor_type2);
    NttTest::init_tensor(*ntt_rhs, -10.f, 10.f);

    // ntt
    std::unique_ptr<tensor_type1> ntt_output1(new tensor_type1);
    ntt::binary<ntt::ops::mul>(*ntt_lhs, *ntt_rhs, *ntt_output1);

    // ort
    auto ort_lhs = NttTest::ntt2ort(*ntt_lhs);
    auto ort_rhs = NttTest::ntt2ort(*ntt_rhs);
    auto ort_output = ortki_Mul(ort_lhs, ort_rhs);

    // compare
    std::unique_ptr<tensor_type1> ntt_output2(new tensor_type1);
    NttTest::ort2ntt(ort_output, *ntt_output2);
    EXPECT_TRUE(NttTest::compare_tensor(*ntt_output1, *ntt_output2));
}

TEST(BinaryTestMulFloat, fixed_fixed_fixed_broadcast_multidirectional) {
    // init
    using tensor_type1 = ntt::tensor<float, ntt::fixed_shape<1, 3, 1, 16>>;
    std::unique_ptr<tensor_type1> ntt_lhs(new tensor_type1);
    NttTest::init_tensor(*ntt_lhs, -10.f, 10.f);

    using tensor_type2 = ntt::tensor<float, ntt::fixed_shape<3, 1, 16, 1>>;
    std::unique_ptr<tensor_type2> ntt_rhs(new tensor_type2);
    NttTest::init_tensor(*ntt_rhs, -10.f, 10.f);

    // ntt
    using tensor_type3 = ntt::tensor<float, ntt::fixed_shape<3, 3, 16, 16>>;
    std::unique_ptr<tensor_type3> ntt_output1(new tensor_type3);
    ntt::binary<ntt::ops::mul>(*ntt_lhs, *ntt_rhs, *ntt_output1);

    // ort
    auto ort_lhs = NttTest::ntt2ort(*ntt_lhs);
    auto ort_rhs = NttTest::ntt2ort(*ntt_rhs);
    auto ort_output = ortki_Mul(ort_lhs, ort_rhs);

    // compare
    std::unique_ptr<tensor_type3> ntt_output2(new tensor_type3);
    NttTest::ort2ntt(ort_output, *ntt_output2);
    EXPECT_TRUE(NttTest::compare_tensor(*ntt_output1, *ntt_output2));
}

TEST(BinaryTestMulFloat, fixed_ranked_ranked) {
    // init
    using tensor_type1 = ntt::tensor<float, ntt::fixed_shape<1, 3, 16, 16>>;
    std::unique_ptr<tensor_type1> ntt_lhs(new tensor_type1);
    NttTest::init_tensor(*ntt_lhs, -10.f, 10.f);

    using tensor_type2 = ntt::tensor<float, ntt::ranked_shape<4>>;
    auto shape = ntt::make_ranked_shape(1, 3, 16, 16);
    std::unique_ptr<tensor_type2> ntt_rhs(new tensor_type2(shape));
    NttTest::init_tensor(*ntt_rhs, -10.f, 10.f);

    // ntt
    std::unique_ptr<tensor_type2> ntt_output1(new tensor_type2(shape));
    ntt::binary<ntt::ops::mul>(*ntt_lhs, *ntt_rhs, *ntt_output1);

    // ort
    auto ort_lhs = NttTest::ntt2ort(*ntt_lhs);
    auto ort_rhs = NttTest::ntt2ort(*ntt_rhs);
    auto ort_output = ortki_Mul(ort_lhs, ort_rhs);

    // compare
    std::unique_ptr<tensor_type2> ntt_output2(new tensor_type2(shape));
    NttTest::ort2ntt(ort_output, *ntt_output2);
    EXPECT_TRUE(NttTest::compare_tensor(*ntt_output1, *ntt_output2));
}

TEST(BinaryTestMulFloat, fixed_ranked_ranked_broadcast_lhs_scalar) {
    // init
    using tensor_type1 = ntt::tensor<float, ntt::fixed_shape<1>>;
    std::unique_ptr<tensor_type1> ntt_lhs(new tensor_type1);
    NttTest::init_tensor(*ntt_lhs, -10.f, 10.f);

    using tensor_type2 = ntt::tensor<float, ntt::ranked_shape<4>>;
    auto shape = ntt::make_ranked_shape(1, 3, 16, 16);
    std::unique_ptr<tensor_type2> ntt_rhs(new tensor_type2(shape));
    NttTest::init_tensor(*ntt_rhs, -10.f, 10.f);

    // ntt
    std::unique_ptr<tensor_type2> ntt_output1(new tensor_type2(shape));
    ntt::binary<ntt::ops::mul>(*ntt_lhs, *ntt_rhs, *ntt_output1);

    // ort
    auto ort_lhs = NttTest::ntt2ort(*ntt_lhs);
    auto ort_rhs = NttTest::ntt2ort(*ntt_rhs);
    auto ort_output = ortki_Mul(ort_lhs, ort_rhs);

    // compare
    std::unique_ptr<tensor_type2> ntt_output2(new tensor_type2(shape));
    NttTest::ort2ntt(ort_output, *ntt_output2);
    EXPECT_TRUE(NttTest::compare_tensor(*ntt_output1, *ntt_output2));
}

TEST(BinaryTestMulFloat, fixed_ranked_ranked_broadcast_rhs_scalar) {
    // init
    using tensor_type1 = ntt::tensor<float, ntt::fixed_shape<1, 3, 16, 16>>;
    std::unique_ptr<tensor_type1> ntt_lhs(new tensor_type1);
    NttTest::init_tensor(*ntt_lhs, -10.f, 10.f);

    using tensor_type2 = ntt::tensor<float, ntt::ranked_shape<1>>;
    auto shape1 = ntt::make_ranked_shape(1);
    std::unique_ptr<tensor_type2> ntt_rhs(new tensor_type2(shape1));
    NttTest::init_tensor(*ntt_rhs, -10.f, 10.f);

    // ntt
    using tensor_type3 = ntt::tensor<float, ntt::ranked_shape<4>>;
    auto shape3 = ntt::make_ranked_shape(1, 3, 16, 16);
    std::unique_ptr<tensor_type3> ntt_output1(new tensor_type3(shape3));
    ntt::binary<ntt::ops::mul>(*ntt_lhs, *ntt_rhs, *ntt_output1);

    // ort
    auto ort_lhs = NttTest::ntt2ort(*ntt_lhs);
    auto ort_rhs = NttTest::ntt2ort(*ntt_rhs);
    auto ort_output = ortki_Mul(ort_lhs, ort_rhs);

    // compare
    std::unique_ptr<tensor_type3> ntt_output2(new tensor_type3(shape3));
    NttTest::ort2ntt(ort_output, *ntt_output2);
    EXPECT_TRUE(NttTest::compare_tensor(*ntt_output1, *ntt_output2));
}

TEST(BinaryTestMulFloat, fixed_ranked_ranked_broadcast_lhs_vector) {
    // init
    using tensor_type1 = ntt::tensor<float, ntt::fixed_shape<16>>;
    std::unique_ptr<tensor_type1> ntt_lhs(new tensor_type1);
    NttTest::init_tensor(*ntt_lhs, -10.f, 10.f);

    using tensor_type2 = ntt::tensor<float, ntt::ranked_shape<4>>;
    auto shape = ntt::make_ranked_shape(1, 3, 16, 16);
    std::unique_ptr<tensor_type2> ntt_rhs(new tensor_type2(shape));
    NttTest::init_tensor(*ntt_rhs, -10.f, 10.f);

    // ntt
    std::unique_ptr<tensor_type2> ntt_output1(new tensor_type2(shape));
    ntt::binary<ntt::ops::mul>(*ntt_lhs, *ntt_rhs, *ntt_output1);

    // ort
    auto ort_lhs = NttTest::ntt2ort(*ntt_lhs);
    auto ort_rhs = NttTest::ntt2ort(*ntt_rhs);
    auto ort_output = ortki_Mul(ort_lhs, ort_rhs);

    // compare
    std::unique_ptr<tensor_type2> ntt_output2(new tensor_type2(shape));
    NttTest::ort2ntt(ort_output, *ntt_output2);
    EXPECT_TRUE(NttTest::compare_tensor(*ntt_output1, *ntt_output2));
}

TEST(BinaryTestMulFloat, fixed_ranked_ranked_broadcast_rhs_vector) {
    // init
    using tensor_type1 = ntt::tensor<float, ntt::fixed_shape<1, 3, 16, 16>>;
    std::unique_ptr<tensor_type1> ntt_lhs(new tensor_type1);
    NttTest::init_tensor(*ntt_lhs, -10.f, 10.f);

    using tensor_type2 = ntt::tensor<float, ntt::ranked_shape<1>>;
    auto shape1 = ntt::make_ranked_shape(16);
    std::unique_ptr<tensor_type2> ntt_rhs(new tensor_type2(shape1));
    NttTest::init_tensor(*ntt_rhs, -10.f, 10.f);

    // ntt
    using tensor_type3 = ntt::tensor<float, ntt::ranked_shape<4>>;
    auto shape3 = ntt::make_ranked_shape(1, 3, 16, 16);
    std::unique_ptr<tensor_type3> ntt_output1(new tensor_type3(shape3));
    ntt::binary<ntt::ops::mul>(*ntt_lhs, *ntt_rhs, *ntt_output1);

    // ort
    auto ort_lhs = NttTest::ntt2ort(*ntt_lhs);
    auto ort_rhs = NttTest::ntt2ort(*ntt_rhs);
    auto ort_output = ortki_Mul(ort_lhs, ort_rhs);

    // compare
    std::unique_ptr<tensor_type3> ntt_output2(new tensor_type3(shape3));
    NttTest::ort2ntt(ort_output, *ntt_output2);
    EXPECT_TRUE(NttTest::compare_tensor(*ntt_output1, *ntt_output2));
}

TEST(BinaryTestMulFloat, fixed_ranked_ranked_broadcast_multidirectional) {
    // init
    using tensor_type1 = ntt::tensor<float, ntt::fixed_shape<1, 3, 1, 16>>;
    std::unique_ptr<tensor_type1> ntt_lhs(new tensor_type1);
    NttTest::init_tensor(*ntt_lhs, -10.f, 10.f);

    using tensor_type2 = ntt::tensor<float, ntt::ranked_shape<4>>;
    auto shape1 = ntt::make_ranked_shape(3, 1, 16, 1);
    std::unique_ptr<tensor_type2> ntt_rhs(new tensor_type2(shape1));
    NttTest::init_tensor(*ntt_rhs, -10.f, 10.f);

    // ntt
    using tensor_type3 = ntt::tensor<float, ntt::ranked_shape<4>>;
    auto shape3 = ntt::make_ranked_shape(3, 3, 16, 16);
    std::unique_ptr<tensor_type3> ntt_output1(new tensor_type3(shape3));
    ntt::binary<ntt::ops::mul>(*ntt_lhs, *ntt_rhs, *ntt_output1);

    // ort
    auto ort_lhs = NttTest::ntt2ort(*ntt_lhs);
    auto ort_rhs = NttTest::ntt2ort(*ntt_rhs);
    auto ort_output = ortki_Mul(ort_lhs, ort_rhs);

    // compare
    std::unique_ptr<tensor_type3> ntt_output2(new tensor_type3(shape3));
    NttTest::ort2ntt(ort_output, *ntt_output2);
    EXPECT_TRUE(NttTest::compare_tensor(*ntt_output1, *ntt_output2));
}

TEST(BinaryTestMulFloat, ranked_fixed_ranked) {
    // init
    using tensor_type1 = ntt::tensor<float, ntt::ranked_shape<4>>;
    auto shape = ntt::make_ranked_shape(1, 3, 16, 16);
    std::unique_ptr<tensor_type1> ntt_lhs(new tensor_type1(shape));
    NttTest::init_tensor(*ntt_lhs, -10.f, 10.f);

    using tensor_type2 = ntt::tensor<float, ntt::fixed_shape<1, 3, 16, 16>>;
    std::unique_ptr<tensor_type2> ntt_rhs(new tensor_type2);
    NttTest::init_tensor(*ntt_rhs, -10.f, 10.f);

    // ntt
    std::unique_ptr<tensor_type1> ntt_output1(new tensor_type1(shape));
    ntt::binary<ntt::ops::mul>(*ntt_lhs, *ntt_rhs, *ntt_output1);

    // ort
    auto ort_lhs = NttTest::ntt2ort(*ntt_lhs);
    auto ort_rhs = NttTest::ntt2ort(*ntt_rhs);
    auto ort_output = ortki_Mul(ort_lhs, ort_rhs);

    // compare
    std::unique_ptr<tensor_type1> ntt_output2(new tensor_type1(shape));
    NttTest::ort2ntt(ort_output, *ntt_output2);
    EXPECT_TRUE(NttTest::compare_tensor(*ntt_output1, *ntt_output2));
}

TEST(BinaryTestMulFloat, ranked_fixed_ranked_broadcast_lhs_scalar) {
    // init
    using tensor_type1 = ntt::tensor<float, ntt::ranked_shape<1>>;
    auto shape1 = ntt::make_ranked_shape(1);
    std::unique_ptr<tensor_type1> ntt_lhs(new tensor_type1(shape1));
    NttTest::init_tensor(*ntt_lhs, -10.f, 10.f);

    using tensor_type2 = ntt::tensor<float, ntt::fixed_shape<1, 3, 16, 16>>;
    std::unique_ptr<tensor_type2> ntt_rhs(new tensor_type2);
    NttTest::init_tensor(*ntt_rhs, -10.f, 10.f);

    // ntt
    using tensor_type3 = ntt::tensor<float, ntt::ranked_shape<4>>;
    auto shape3 = ntt::make_ranked_shape(1, 3, 16, 16);
    std::unique_ptr<tensor_type3> ntt_output1(new tensor_type3(shape3));
    ntt::binary<ntt::ops::mul>(*ntt_lhs, *ntt_rhs, *ntt_output1);

    // ort
    auto ort_lhs = NttTest::ntt2ort(*ntt_lhs);
    auto ort_rhs = NttTest::ntt2ort(*ntt_rhs);
    auto ort_output = ortki_Mul(ort_lhs, ort_rhs);

    // compare
    std::unique_ptr<tensor_type3> ntt_output2(new tensor_type3(shape3));
    NttTest::ort2ntt(ort_output, *ntt_output2);
    EXPECT_TRUE(NttTest::compare_tensor(*ntt_output1, *ntt_output2));
}

TEST(BinaryTestMulFloat, ranked_fixed_ranked_broadcast_rhs_scalar) {
    // init
    using tensor_type1 = ntt::tensor<float, ntt::ranked_shape<4>>;
    auto shape1 = ntt::make_ranked_shape(1, 3, 16, 16);
    std::unique_ptr<tensor_type1> ntt_lhs(new tensor_type1(shape1));
    NttTest::init_tensor(*ntt_lhs, -10.f, 10.f);

    using tensor_type2 = ntt::tensor<float, ntt::fixed_shape<1>>;
    std::unique_ptr<tensor_type2> ntt_rhs(new tensor_type2);
    NttTest::init_tensor(*ntt_rhs, -10.f, 10.f);

    // ntt
    using tensor_type3 = ntt::tensor<float, ntt::ranked_shape<4>>;
    auto shape3 = ntt::make_ranked_shape(1, 3, 16, 16);
    std::unique_ptr<tensor_type3> ntt_output1(new tensor_type3(shape3));
    ntt::binary<ntt::ops::mul>(*ntt_lhs, *ntt_rhs, *ntt_output1);

    // ort
    auto ort_lhs = NttTest::ntt2ort(*ntt_lhs);
    auto ort_rhs = NttTest::ntt2ort(*ntt_rhs);
    auto ort_output = ortki_Mul(ort_lhs, ort_rhs);

    // compare
    std::unique_ptr<tensor_type3> ntt_output2(new tensor_type3(shape3));
    NttTest::ort2ntt(ort_output, *ntt_output2);
    EXPECT_TRUE(NttTest::compare_tensor(*ntt_output1, *ntt_output2));
}

TEST(BinaryTestMulFloat, ranked_fixed_ranked_broadcast_lhs_vector) {
    // init
    using tensor_type1 = ntt::tensor<float, ntt::ranked_shape<1>>;
    auto shape1 = ntt::make_ranked_shape(16);
    std::unique_ptr<tensor_type1> ntt_lhs(new tensor_type1(shape1));
    NttTest::init_tensor(*ntt_lhs, -10.f, 10.f);

    using tensor_type2 = ntt::tensor<float, ntt::fixed_shape<1, 3, 16, 16>>;
    std::unique_ptr<tensor_type2> ntt_rhs(new tensor_type2);
    NttTest::init_tensor(*ntt_rhs, -10.f, 10.f);

    // ntt
    using tensor_type3 = ntt::tensor<float, ntt::ranked_shape<4>>;
    auto shape3 = ntt::make_ranked_shape(1, 3, 16, 16);
    std::unique_ptr<tensor_type3> ntt_output1(new tensor_type3(shape3));
    ntt::binary<ntt::ops::mul>(*ntt_lhs, *ntt_rhs, *ntt_output1);

    // ort
    auto ort_lhs = NttTest::ntt2ort(*ntt_lhs);
    auto ort_rhs = NttTest::ntt2ort(*ntt_rhs);
    auto ort_output = ortki_Mul(ort_lhs, ort_rhs);

    // compare
    std::unique_ptr<tensor_type3> ntt_output2(new tensor_type3(shape3));
    NttTest::ort2ntt(ort_output, *ntt_output2);
    EXPECT_TRUE(NttTest::compare_tensor(*ntt_output1, *ntt_output2));
}

TEST(BinaryTestMulFloat, ranked_fixed_ranked_broadcast_rhs_vector) {
    // init
    using tensor_type1 = ntt::tensor<float, ntt::ranked_shape<4>>;
    auto shape1 = ntt::make_ranked_shape(1, 3, 16, 16);
    std::unique_ptr<tensor_type1> ntt_lhs(new tensor_type1(shape1));
    NttTest::init_tensor(*ntt_lhs, -10.f, 10.f);

    using tensor_type2 = ntt::tensor<float, ntt::fixed_shape<16>>;
    std::unique_ptr<tensor_type2> ntt_rhs(new tensor_type2);
    NttTest::init_tensor(*ntt_rhs, -10.f, 10.f);

    // ntt
    using tensor_type3 = ntt::tensor<float, ntt::ranked_shape<4>>;
    auto shape3 = ntt::make_ranked_shape(1, 3, 16, 16);
    std::unique_ptr<tensor_type3> ntt_output1(new tensor_type3(shape3));
    ntt::binary<ntt::ops::mul>(*ntt_lhs, *ntt_rhs, *ntt_output1);

    // ort
    auto ort_lhs = NttTest::ntt2ort(*ntt_lhs);
    auto ort_rhs = NttTest::ntt2ort(*ntt_rhs);
    auto ort_output = ortki_Mul(ort_lhs, ort_rhs);

    // compare
    std::unique_ptr<tensor_type3> ntt_output2(new tensor_type3(shape3));
    NttTest::ort2ntt(ort_output, *ntt_output2);
    EXPECT_TRUE(NttTest::compare_tensor(*ntt_output1, *ntt_output2));
}

TEST(BinaryTestMulFloat, ranked_fixed_ranked_broadcast_multidirectional) {
    // init
    using tensor_type1 = ntt::tensor<float, ntt::ranked_shape<4>>;
    auto shape1 = ntt::make_ranked_shape(1, 3, 1, 16);
    std::unique_ptr<tensor_type1> ntt_lhs(new tensor_type1(shape1));
    NttTest::init_tensor(*ntt_lhs, -10.f, 10.f);

    using tensor_type2 = ntt::tensor<float, ntt::fixed_shape<3, 1, 16, 1>>;
    std::unique_ptr<tensor_type2> ntt_rhs(new tensor_type2);
    NttTest::init_tensor(*ntt_rhs, -10.f, 10.f);

    // ntt
    using tensor_type3 = ntt::tensor<float, ntt::ranked_shape<4>>;
    auto shape3 = ntt::make_ranked_shape(3, 3, 16, 16);
    std::unique_ptr<tensor_type3> ntt_output1(new tensor_type3(shape3));
    ntt::binary<ntt::ops::mul>(*ntt_lhs, *ntt_rhs, *ntt_output1);

    // ort
    auto ort_lhs = NttTest::ntt2ort(*ntt_lhs);
    auto ort_rhs = NttTest::ntt2ort(*ntt_rhs);
    auto ort_output = ortki_Mul(ort_lhs, ort_rhs);

    // compare
    std::unique_ptr<tensor_type3> ntt_output2(new tensor_type3(shape3));
    NttTest::ort2ntt(ort_output, *ntt_output2);
    EXPECT_TRUE(NttTest::compare_tensor(*ntt_output1, *ntt_output2));
}

TEST(BinaryTestMulFloat, ranked_ranked_ranked) {
    // init
    using tensor_type = ntt::tensor<float, ntt::ranked_shape<4>>;
    auto shape = ntt::make_ranked_shape(1, 3, 16, 16);
    std::unique_ptr<tensor_type> ntt_lhs(new tensor_type(shape));
    NttTest::init_tensor(*ntt_lhs, -10.f, 10.f);

    std::unique_ptr<tensor_type> ntt_rhs(new tensor_type(shape));
    NttTest::init_tensor(*ntt_rhs, -10.f, 10.f);

    // ntt
    std::unique_ptr<tensor_type> ntt_output1(new tensor_type(shape));
    ntt::binary<ntt::ops::mul>(*ntt_lhs, *ntt_rhs, *ntt_output1);

    // ort
    auto ort_lhs = NttTest::ntt2ort(*ntt_lhs);
    auto ort_rhs = NttTest::ntt2ort(*ntt_rhs);
    auto ort_output = ortki_Mul(ort_lhs, ort_rhs);

    // compare
    std::unique_ptr<tensor_type> ntt_output2(new tensor_type(shape));
    NttTest::ort2ntt(ort_output, *ntt_output2);
    EXPECT_TRUE(NttTest::compare_tensor(*ntt_output1, *ntt_output2));
}

TEST(BinaryTestMulFloat, ranked_ranked_ranked_broadcast_lhs_scalar) {
    // init
    using tensor_type1 = ntt::tensor<float, ntt::ranked_shape<1>>;
    auto shape1 = ntt::make_ranked_shape(1);
    std::unique_ptr<tensor_type1> ntt_lhs(new tensor_type1(shape1));
    NttTest::init_tensor(*ntt_lhs, -10.f, 10.f);

    using tensor_type2 = ntt::tensor<float, ntt::ranked_shape<4>>;
    auto shape2 = ntt::make_ranked_shape(1, 3, 16, 16);
    std::unique_ptr<tensor_type2> ntt_rhs(new tensor_type2(shape2));
    NttTest::init_tensor(*ntt_rhs, -10.f, 10.f);

    // ntt
    std::unique_ptr<tensor_type2> ntt_output1(new tensor_type2(shape2));
    ntt::binary<ntt::ops::mul>(*ntt_lhs, *ntt_rhs, *ntt_output1);

    // ort
    auto ort_lhs = NttTest::ntt2ort(*ntt_lhs);
    auto ort_rhs = NttTest::ntt2ort(*ntt_rhs);
    auto ort_output = ortki_Mul(ort_lhs, ort_rhs);

    // compare
    std::unique_ptr<tensor_type2> ntt_output2(new tensor_type2(shape2));
    NttTest::ort2ntt(ort_output, *ntt_output2);
    EXPECT_TRUE(NttTest::compare_tensor(*ntt_output1, *ntt_output2));
}

TEST(BinaryTestMulFloat, ranked_ranked_ranked_broadcast_rhs_scalar) {
    // init
    using tensor_type1 = ntt::tensor<float, ntt::ranked_shape<4>>;
    auto shape1 = ntt::make_ranked_shape(1, 3, 16, 16);
    std::unique_ptr<tensor_type1> ntt_lhs(new tensor_type1(shape1));
    NttTest::init_tensor(*ntt_lhs, -10.f, 10.f);

    using tensor_type2 = ntt::tensor<float, ntt::ranked_shape<1>>;
    auto shape2 = ntt::make_ranked_shape(1);
    std::unique_ptr<tensor_type2> ntt_rhs(new tensor_type2(shape2));
    NttTest::init_tensor(*ntt_rhs, -10.f, 10.f);

    // ntt
    std::unique_ptr<tensor_type1> ntt_output1(new tensor_type1(shape1));
    ntt::binary<ntt::ops::mul>(*ntt_lhs, *ntt_rhs, *ntt_output1);

    // ort
    auto ort_lhs = NttTest::ntt2ort(*ntt_lhs);
    auto ort_rhs = NttTest::ntt2ort(*ntt_rhs);
    auto ort_output = ortki_Mul(ort_lhs, ort_rhs);

    // compare
    std::unique_ptr<tensor_type1> ntt_output2(new tensor_type1(shape1));
    NttTest::ort2ntt(ort_output, *ntt_output2);
    EXPECT_TRUE(NttTest::compare_tensor(*ntt_output1, *ntt_output2));
}

TEST(BinaryTestMulFloat, ranked_ranked_ranked_broadcast_lhs_vector) {
    // init
    using tensor_type1 = ntt::tensor<float, ntt::ranked_shape<1>>;
    auto shape1 = ntt::make_ranked_shape(16);
    std::unique_ptr<tensor_type1> ntt_lhs(new tensor_type1(shape1));
    NttTest::init_tensor(*ntt_lhs, -10.f, 10.f);

    using tensor_type2 = ntt::tensor<float, ntt::ranked_shape<4>>;
    auto shape2 = ntt::make_ranked_shape(1, 3, 16, 16);
    std::unique_ptr<tensor_type2> ntt_rhs(new tensor_type2(shape2));
    NttTest::init_tensor(*ntt_rhs, -10.f, 10.f);

    // ntt
    std::unique_ptr<tensor_type2> ntt_output1(new tensor_type2(shape2));
    ntt::binary<ntt::ops::mul>(*ntt_lhs, *ntt_rhs, *ntt_output1);

    // ort
    auto ort_lhs = NttTest::ntt2ort(*ntt_lhs);
    auto ort_rhs = NttTest::ntt2ort(*ntt_rhs);
    auto ort_output = ortki_Mul(ort_lhs, ort_rhs);

    // compare
    std::unique_ptr<tensor_type2> ntt_output2(new tensor_type2(shape2));
    NttTest::ort2ntt(ort_output, *ntt_output2);
    EXPECT_TRUE(NttTest::compare_tensor(*ntt_output1, *ntt_output2));
}

TEST(BinaryTestMulFloat, ranked_ranked_ranked_broadcast_rhs_vector) {
    // init
    using tensor_type1 = ntt::tensor<float, ntt::ranked_shape<4>>;
    auto shape1 = ntt::make_ranked_shape(1, 3, 16, 16);
    std::unique_ptr<tensor_type1> ntt_lhs(new tensor_type1(shape1));
    NttTest::init_tensor(*ntt_lhs, -10.f, 10.f);

    using tensor_type2 = ntt::tensor<float, ntt::ranked_shape<1>>;
    auto shape2 = ntt::make_ranked_shape(16);
    std::unique_ptr<tensor_type2> ntt_rhs(new tensor_type2(shape2));
    NttTest::init_tensor(*ntt_rhs, -10.f, 10.f);

    // ntt
    std::unique_ptr<tensor_type1> ntt_output1(new tensor_type1(shape1));
    ntt::binary<ntt::ops::mul>(*ntt_lhs, *ntt_rhs, *ntt_output1);

    // ort
    auto ort_lhs = NttTest::ntt2ort(*ntt_lhs);
    auto ort_rhs = NttTest::ntt2ort(*ntt_rhs);
    auto ort_output = ortki_Mul(ort_lhs, ort_rhs);

    // compare
    std::unique_ptr<tensor_type1> ntt_output2(new tensor_type1(shape1));
    NttTest::ort2ntt(ort_output, *ntt_output2);
    EXPECT_TRUE(NttTest::compare_tensor(*ntt_output1, *ntt_output2));
}

TEST(BinaryTestMulFloat, ranked_ranked_ranked_broadcast_multidirectional) {
    // init
    using tensor_type1 = ntt::tensor<float, ntt::ranked_shape<4>>;
    auto shape1 = ntt::make_ranked_shape(1, 3, 1, 16);
    std::unique_ptr<tensor_type1> ntt_lhs(new tensor_type1(shape1));
    NttTest::init_tensor(*ntt_lhs, -10.f, 10.f);

    using tensor_type2 = ntt::tensor<float, ntt::ranked_shape<4>>;
    auto shape2 = ntt::make_ranked_shape(3, 1, 16, 1);
    std::unique_ptr<tensor_type2> ntt_rhs(new tensor_type2(shape2));
    NttTest::init_tensor(*ntt_rhs, -10.f, 10.f);

    // ntt
    using tensor_type3 = ntt::tensor<float, ntt::ranked_shape<4>>;
    auto shape3 = ntt::make_ranked_shape(3, 3, 16, 16);
    std::unique_ptr<tensor_type3> ntt_output1(new tensor_type3(shape3));
    ntt::binary<ntt::ops::mul>(*ntt_lhs, *ntt_rhs, *ntt_output1);

    // ort
    auto ort_lhs = NttTest::ntt2ort(*ntt_lhs);
    auto ort_rhs = NttTest::ntt2ort(*ntt_rhs);
    auto ort_output = ortki_Mul(ort_lhs, ort_rhs);

    // compare
    std::unique_ptr<tensor_type3> ntt_output2(new tensor_type3(shape3));
    NttTest::ort2ntt(ort_output, *ntt_output2);
    EXPECT_TRUE(NttTest::compare_tensor(*ntt_output1, *ntt_output2));
}

TEST(BinaryTestMulFloat, vecotor_8) {
    // init
    ntt::vector<float, 8> ntt_lhs, ntt_rhs;
    NttTest::init_tensor(ntt_lhs, -10.f, 10.f);
    NttTest::init_tensor(ntt_rhs, -10.f, 10.f);

    // ntt
    auto ntt_output1 = ntt::mul(ntt_lhs, ntt_rhs);

    // ort
    auto ort_lhs = NttTest::ntt2ort(ntt_lhs);
    auto ort_rhs = NttTest::ntt2ort(ntt_rhs);
    auto ort_output = ortki_Mul(ort_lhs, ort_rhs);

    // compare
    ntt::vector<float, 8> ntt_output2;
    NttTest::ort2ntt(ort_output, ntt_output2);
    EXPECT_TRUE(NttTest::compare_tensor(ntt_output1, ntt_output2));
}

int main(int argc, char *argv[]) {
    ::testing::InitGoogleTest(&argc, argv);
    return RUN_ALL_TESTS();
}
