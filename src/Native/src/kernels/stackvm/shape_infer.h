/* This file is generated by tools/stackvm_gen/IsaGen at 2022/5/14 下午6:47:34
* +08:00.
*
* Copyright 2019-2021 Canaan Inc.
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
#pragma once
#include <nncase/kernels/kernel_context.h>
#include <nncase/runtime/datatypes.h>
#include <nncase/runtime/error.h>
#include <nncase/runtime/result.h>
#include <nncase/runtime/stackvm/opcode.h>
#include <nncase/tensor.h>
#include <nncase/value.h>
#include <numeric>

BEGIN_NS_NNCASE_KERNELS_MODULE(stackvm)

dims_t gather_infer_shape(const dims_t& in_shape, const dims_t& index_shape, int axis) {
    auto new_shape = in_shape;
    new_shape.erase(new_shape.begin() + axis);
    new_shape.insert(new_shape.begin() + axis, index_shape.begin(), index_shape.end());
    return new_shape;
}

dims_t gather_nd_infer_shape(const dims_t& in_shape, const dims_t& index_shape, size_t batch_dims) {
    auto new_shape = index_shape;
    new_shape.pop_back();
    new_shape.insert(new_shape.end(), in_shape.begin() + index_shape.back() + batch_dims, in_shape.end());
    if(new_shape.empty())
    {
        new_shape.push_back(1);
    }
    return new_shape;
}

dims_t slice_infer_shape(const dims_t &in_shape, const axes_t& begins, const axes_t& ends,
                   const axes_t& strides) {
    auto new_shape = dims_t();
    for (size_t i = 0; i < strides.size(); i++) {
        auto stride = strides[i];
        auto begin_val = begins[i];
        auto end_val = std::min(ends[i], (int64_t)in_shape[i]);
        auto dim = (int)std::ceil(
            ((float)std::abs(end_val - begin_val) / (float)std::abs(stride)));
        new_shape.push_back(dim);
    }

    return new_shape.size() ? new_shape : dims_t{1};
}

std::vector<dims_t> split_shape_infer(const dims_t& in_shape, size_t axis, const dims_t& sections)
{
    auto result = std::vector<dims_t>();
    for (int i = 0; i < sections.size(); ++i) {
        auto shape = in_shape;
        shape[axis] = sections[i];
        result.push_back(shape);
    }
    return result;
}

dims_t reshape_shape_infer(const dims_t &in_shape, const axes_t &new_shape)
{
    auto neg_index = -1;
    auto sum = 1;
    for (int i = 0; i < new_shape.size(); ++i) {
        if(new_shape[i] != -1) {
            sum *= new_shape[i];
        } else {
            neg_index = i;
        }
    }
    if(neg_index == -1)
    {
        return dims_t(new_shape.begin(), new_shape.end());
    }
    else
    {
        auto result_shape = new_shape;
        auto in_size = std::accumulate(in_shape.begin(), in_shape.end(), 1, std::multiplies<int64_t>{});
        result_shape[neg_index] = in_size / sum;
        return dims_t(result_shape.begin(), result_shape.end());
    }
}

dims_t stack_infer_shape(dims_t shape0, int input_count, int axis) {
    shape0.insert(shape0.begin() + axis, input_count);
    return shape0;
}

dims_t unsqueeze_infer_shape(const dims_t& in_shape, const dims_t& axes) {
    auto size = in_shape.size() + axes.size();
    auto new_shape = dims_t(size);
    for (auto i = 0, j = 0; i < size; i++) {
        new_shape[i] = std::find(axes.begin(), axes.end(), i) == axes.end() ? in_shape[j++] : 1;
    }
    return new_shape;
}

END_NS_NNCASE_KERNELS_MODULE